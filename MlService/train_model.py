"""Train and evaluate the 30-day failure classifier without time leakage."""

from __future__ import annotations

import json
import platform
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

import joblib
import numpy as np
import pandas as pd
import sklearn
from sklearn.calibration import CalibratedClassifierCV, calibration_curve
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestClassifier
from sklearn.impute import SimpleImputer
from sklearn.metrics import (
    brier_score_loss,
    confusion_matrix,
    f1_score,
    log_loss,
    precision_score,
    recall_score,
    roc_auc_score,
)
from sklearn.model_selection import GroupShuffleSplit, TimeSeriesSplit, train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder

from model_utils import CATEGORICAL_FEATURES, FEATURE_NAMES, NUMERIC_FEATURES

BASE_DIR = Path(__file__).resolve().parent
DATA_DIR = BASE_DIR / "data"
MODEL_DIR = BASE_DIR / "model_artifacts"
DATA_PATH = DATA_DIR / "11_ml_egitim_verisi.csv"
MODEL_PATH = MODEL_DIR / "model.pkl"
FEATURE_CONFIG_PATH = MODEL_DIR / "feature_config.json"
MODEL_METADATA_PATH = MODEL_DIR / "model_metadata.json"
EVALUATION_REPORT_PATH = MODEL_DIR / "evaluation_report.json"

TARGET_COLUMN = "gelecek_30g_ariza"
DATE_COLUMN = "anlik_tarih"
GROUP_COLUMN = "makine_id"
MODEL_VERSION = "rf-v2-time-aware"
RANDOM_STATE = 42


def create_pipeline() -> Pipeline:
    numeric_pipeline = Pipeline([
        ("imputer", SimpleImputer(strategy="median")),
    ])
    categorical_pipeline = Pipeline([
        ("imputer", SimpleImputer(strategy="constant", fill_value="Bilinmiyor")),
        ("encoder", OneHotEncoder(handle_unknown="ignore")),
    ])
    preprocess = ColumnTransformer([
        ("numeric", numeric_pipeline, NUMERIC_FEATURES),
        ("categorical", categorical_pipeline, CATEGORICAL_FEATURES),
    ])
    classifier = RandomForestClassifier(
        n_estimators=300,
        max_depth=8,
        min_samples_leaf=5,
        class_weight="balanced",
        random_state=RANDOM_STATE,
        n_jobs=-1,
    )
    return Pipeline([("preprocess", preprocess), ("classifier", classifier)])


def score_predictions(y_true: pd.Series, probabilities: np.ndarray) -> dict[str, Any]:
    predicted = (probabilities >= 0.5).astype(int)
    return {
        "rowCount": int(len(y_true)),
        "positiveRate": round(float(y_true.mean()), 6),
        "rocAuc": round(float(roc_auc_score(y_true, probabilities)), 6),
        "precision": round(float(precision_score(y_true, predicted, zero_division=0)), 6),
        "recall": round(float(recall_score(y_true, predicted, zero_division=0)), 6),
        "f1": round(float(f1_score(y_true, predicted, zero_division=0)), 6),
        "brierScore": round(float(brier_score_loss(y_true, probabilities)), 6),
        "logLoss": round(float(log_loss(y_true, probabilities, labels=[0, 1])), 6),
        "confusionMatrix": confusion_matrix(y_true, predicted, labels=[0, 1]).tolist(),
    }


def fit_and_score(
    frame: pd.DataFrame, train_indices: Iterable[int], test_indices: Iterable[int]
) -> dict[str, Any]:
    train_indices = np.asarray(list(train_indices))
    test_indices = np.asarray(list(test_indices))
    model = create_pipeline()
    model.fit(frame.iloc[train_indices][FEATURE_NAMES], frame.iloc[train_indices][TARGET_COLUMN])
    probabilities = model.predict_proba(frame.iloc[test_indices][FEATURE_NAMES])[:, 1]
    result = score_predictions(frame.iloc[test_indices][TARGET_COLUMN], probabilities)
    result.update({
        "trainRowCount": int(len(train_indices)),
        "trainDateMax": frame.iloc[train_indices][DATE_COLUMN].max().isoformat(),
        "testDateMin": frame.iloc[test_indices][DATE_COLUMN].min().isoformat(),
    })
    return result


def chronological_split(frame: pd.DataFrame, train_fraction: float = 0.8):
    unique_dates = np.array(sorted(frame[DATE_COLUMN].dropna().unique()))
    if len(unique_dates) < 3:
        raise ValueError("Zaman tabanlı değerlendirme için en az üç farklı tarih gereklidir.")
    boundary_index = min(max(int(len(unique_dates) * train_fraction), 1), len(unique_dates) - 1)
    boundary = unique_dates[boundary_index]
    train_indices = np.flatnonzero((frame[DATE_COLUMN] < boundary).to_numpy())
    test_indices = np.flatnonzero((frame[DATE_COLUMN] >= boundary).to_numpy())
    return train_indices, test_indices


def time_series_splits(frame: pd.DataFrame, split_count: int = 5):
    unique_dates = np.array(sorted(frame[DATE_COLUMN].dropna().unique()))
    actual_split_count = min(split_count, len(unique_dates) - 1)
    if actual_split_count < 2:
        raise ValueError("TimeSeriesSplit için yeterli farklı tarih bulunmuyor.")
    splitter = TimeSeriesSplit(n_splits=actual_split_count)
    for train_date_indices, test_date_indices in splitter.split(unique_dates):
        train_dates = unique_dates[train_date_indices]
        test_dates = unique_dates[test_date_indices]
        train_rows = np.flatnonzero(frame[DATE_COLUMN].isin(train_dates).to_numpy())
        test_rows = np.flatnonzero(frame[DATE_COLUMN].isin(test_dates).to_numpy())
        yield train_rows, test_rows


def average_fold_metrics(folds: list[dict[str, Any]]) -> dict[str, Any]:
    metric_names = ["rocAuc", "precision", "recall", "f1", "brierScore", "logLoss"]
    return {
        name: round(float(np.mean([fold[name] for fold in folds])), 6)
        for name in metric_names
    }


def calibration_comparison(
    frame: pd.DataFrame, train_indices: np.ndarray, test_indices: np.ndarray
) -> tuple[dict[str, Any], bool]:
    x_train = frame.iloc[train_indices][FEATURE_NAMES]
    y_train = frame.iloc[train_indices][TARGET_COLUMN]
    x_test = frame.iloc[test_indices][FEATURE_NAMES]
    y_test = frame.iloc[test_indices][TARGET_COLUMN]

    raw_model = create_pipeline()
    raw_model.fit(x_train, y_train)
    raw_probabilities = raw_model.predict_proba(x_test)[:, 1]

    train_frame = frame.iloc[train_indices].reset_index(drop=True)
    calibration_cv = list(time_series_splits(train_frame, split_count=3))
    calibrated_model = CalibratedClassifierCV(
        estimator=create_pipeline(), method="sigmoid", cv=calibration_cv, n_jobs=-1
    )
    calibrated_model.fit(x_train, y_train)
    calibrated_probabilities = calibrated_model.predict_proba(x_test)[:, 1]

    raw_scores = score_predictions(y_test, raw_probabilities)
    calibrated_scores = score_predictions(y_test, calibrated_probabilities)
    fraction_positive, mean_predicted = calibration_curve(
        y_test, calibrated_probabilities, n_bins=10, strategy="quantile"
    )
    use_calibrated = (
        calibrated_scores["brierScore"] < raw_scores["brierScore"]
        and calibrated_scores["logLoss"] <= raw_scores["logLoss"] + 0.01
    )
    return ({
        "raw": raw_scores,
        "sigmoidCalibrated": calibrated_scores,
        "calibrationCurve": {
            "meanPredictedProbability": [round(float(x), 6) for x in mean_predicted],
            "fractionOfPositives": [round(float(x), 6) for x in fraction_positive],
        },
        "selected": "sigmoid" if use_calibrated else "raw",
        "selectionRule": "Lower Brier score without more than 0.01 log-loss degradation.",
    }, use_calibrated)


def global_feature_importance(model: Pipeline) -> list[dict[str, Any]]:
    preprocess = model.named_steps["preprocess"]
    transformed_names = preprocess.get_feature_names_out()
    values = model.named_steps["classifier"].feature_importances_
    aggregated = {name: 0.0 for name in FEATURE_NAMES}
    for transformed_name, importance in zip(transformed_names, values, strict=True):
        raw_name = transformed_name.split("__", 1)[-1]
        matching_feature = next(
            (name for name in FEATURE_NAMES if raw_name == name or raw_name.startswith(f"{name}_")),
            None,
        )
        if matching_feature:
            aggregated[matching_feature] += float(importance)
    return [
        {"feature": name, "importance": round(value, 8)}
        for name, value in sorted(aggregated.items(), key=lambda item: item[1], reverse=True)
    ]


def write_artifact_metadata(
    frame: pd.DataFrame,
    report: dict[str, Any],
    importance: list[dict[str, Any]],
    calibrated: bool,
) -> None:
    numeric_stats = {
        name: {
            "min": round(float(frame[name].min()), 6),
            "max": round(float(frame[name].max()), 6),
            "mean": round(float(frame[name].mean()), 6),
            "std": round(float(frame[name].std()), 6),
            "median": round(float(frame[name].median()), 6),
        }
        for name in NUMERIC_FEATURES
    }
    categories = {
        name: sorted(str(value) for value in frame[name].dropna().unique())
        for name in CATEGORICAL_FEATURES
    }
    feature_config = {
        "modelVersion": MODEL_VERSION,
        "target": TARGET_COLUMN,
        "features": [
            {
                "name": name,
                "type": "string" if name in CATEGORICAL_FEATURES else "number",
                "nullable": True,
                "requiredForModel": True,
            }
            for name in FEATURE_NAMES
        ],
        "numericFeatureStats": numeric_stats,
        "categories": categories,
        "minimumCompletenessRate": 50.0,
    }
    metadata = {
        "modelVersion": MODEL_VERSION,
        "trainedAt": datetime.now(timezone.utc).isoformat(),
        "trainingRows": int(len(frame)),
        "machineCount": int(frame[GROUP_COLUMN].nunique()),
        "pythonVersion": platform.python_version(),
        "scikitLearnVersion": sklearn.__version__,
        "calibrated": calibrated,
        "featureImportance": importance,
        "evaluationSummary": {
            "randomSplit": report["randomSplit"],
            "timeSplit": report["timeSplit"],
            "timeSeriesSplitAverage": report["timeSeriesSplitAverage"],
            "unseenMachine": report["unseenMachine"],
        },
        "probabilityNotice": (
            "failureProbability modelin 30 günlük risk çıktısıdır; saha koşullarında kesin arıza "
            "olasılığı olarak yorumlanmamalıdır."
        ),
    }
    FEATURE_CONFIG_PATH.write_text(
        json.dumps(feature_config, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    MODEL_METADATA_PATH.write_text(
        json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    EVALUATION_REPORT_PATH.write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )


def main() -> None:
    if not DATA_PATH.is_file():
        raise FileNotFoundError(f"Eğitim verisi bulunamadı: {DATA_PATH.resolve()}")
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    frame = pd.read_csv(DATA_PATH)
    required_columns = set(FEATURE_NAMES + [TARGET_COLUMN, DATE_COLUMN, GROUP_COLUMN])
    missing_columns = sorted(required_columns.difference(frame.columns))
    if missing_columns:
        raise ValueError(f"Eğitim verisinde zorunlu kolonlar eksik: {missing_columns}")
    frame[DATE_COLUMN] = pd.to_datetime(frame[DATE_COLUMN], errors="raise")
    frame = frame.sort_values([DATE_COLUMN, GROUP_COLUMN]).reset_index(drop=True)

    all_indices = np.arange(len(frame))
    random_train, random_test = train_test_split(
        all_indices,
        test_size=0.2,
        random_state=RANDOM_STATE,
        stratify=frame[TARGET_COLUMN],
    )
    time_train, time_test = chronological_split(frame)
    group_splitter = GroupShuffleSplit(n_splits=1, test_size=0.2, random_state=RANDOM_STATE)
    group_train, group_test = next(
        group_splitter.split(frame[FEATURE_NAMES], frame[TARGET_COLUMN], frame[GROUP_COLUMN])
    )

    time_folds = [fit_and_score(frame, train, test) for train, test in time_series_splits(frame)]
    report: dict[str, Any] = {
        "dataset": {
            "path": DATA_PATH.relative_to(BASE_DIR).as_posix(),
            "rows": int(len(frame)),
            "machines": int(frame[GROUP_COLUMN].nunique()),
            "dateMin": frame[DATE_COLUMN].min().isoformat(),
            "dateMax": frame[DATE_COLUMN].max().isoformat(),
        },
        "randomSplit": fit_and_score(frame, random_train, random_test),
        "timeSplit": fit_and_score(frame, time_train, time_test),
        "timeSeriesSplit": time_folds,
        "timeSeriesSplitAverage": average_fold_metrics(time_folds),
        "unseenMachine": fit_and_score(frame, group_train, group_test),
    }
    report["unseenMachine"]["testMachineCount"] = int(
        frame.iloc[group_test][GROUP_COLUMN].nunique()
    )
    calibration, use_calibrated = calibration_comparison(frame, time_train, time_test)
    report["calibration"] = calibration

    importance_model = create_pipeline()
    importance_model.fit(frame[FEATURE_NAMES], frame[TARGET_COLUMN])
    importance = global_feature_importance(importance_model)

    if use_calibrated:
        final_cv = list(time_series_splits(frame, split_count=5))
        final_model = CalibratedClassifierCV(
            estimator=create_pipeline(), method="sigmoid", cv=final_cv, n_jobs=-1
        )
        final_model.fit(frame[FEATURE_NAMES], frame[TARGET_COLUMN])
    else:
        final_model = importance_model
    joblib.dump(final_model, MODEL_PATH)
    write_artifact_metadata(frame, report, importance, use_calibrated)

    print(f"Model kaydedildi: {MODEL_PATH.resolve()}")
    print(json.dumps({
        "randomSplit": report["randomSplit"],
        "timeSplit": report["timeSplit"],
        "timeSeriesSplitAverage": report["timeSeriesSplitAverage"],
        "unseenMachine": report["unseenMachine"],
        "calibrationSelected": report["calibration"]["selected"],
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
