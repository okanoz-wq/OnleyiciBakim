"""Flask API wrapping the trained predictive-maintenance pipeline."""

from __future__ import annotations

import hmac
import json
import logging
import os
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import joblib
import numpy as np
import pandas as pd
from flask import Flask, jsonify, request
from pydantic import ValidationError

from model_utils import (
    CATEGORICAL_FEATURES,
    FEATURE_NAMES,
    build_model_explanation,
    build_recommended_actions,
    build_rule_based_explanation,
    calculate_completeness,
    risk_level_from_score,
)
from schemas import PredictionRequest

BASE_DIR = Path(__file__).resolve().parent
MODEL_DIR = BASE_DIR / "model_artifacts"
MODEL_PATH = MODEL_DIR / "model.pkl"
FEATURE_CONFIG_PATH = MODEL_DIR / "feature_config.json"
MODEL_METADATA_PATH = MODEL_DIR / "model_metadata.json"

logging.basicConfig(
    level=os.getenv("ML_LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)
LOGGER = logging.getLogger("onleyici_bakim.ml")

app = Flask(__name__)
app.config["MAX_CONTENT_LENGTH"] = int(os.getenv("ML_MAX_REQUEST_BYTES", "65536"))

MODEL: Any | None = None
FEATURE_CONFIG: dict[str, Any] = {}
MODEL_METADATA: dict[str, Any] = {}
RESOURCE_ERROR: str | None = None


def load_resources(
    model_path: Path = MODEL_PATH,
    feature_config_path: Path = FEATURE_CONFIG_PATH,
    metadata_path: Path = MODEL_METADATA_PATH,
) -> None:
    """Load artifacts using module-relative paths and retain a safe health error."""
    global MODEL, FEATURE_CONFIG, MODEL_METADATA, RESOURCE_ERROR
    MODEL = None
    RESOURCE_ERROR = None
    try:
        for path in (model_path, feature_config_path, metadata_path):
            if not path.is_file():
                raise FileNotFoundError(f"Gerekli ML dosyası bulunamadı: {path.resolve()}")
        with feature_config_path.open("r", encoding="utf-8") as handle:
            FEATURE_CONFIG = json.load(handle)
        with metadata_path.open("r", encoding="utf-8") as handle:
            MODEL_METADATA = json.load(handle)
        MODEL = joblib.load(model_path)
        LOGGER.info("ML modeli yüklendi: %s", model_path.resolve())
    except Exception as exc:  # service remains alive so /health can explain status
        RESOURCE_ERROR = str(exc)
        LOGGER.exception("ML kaynakları yüklenemedi. Model yolu: %s", model_path.resolve())


def error_response(
    code: str,
    message: str,
    status: int,
    *,
    completeness: float | None = None,
    details: list[dict[str, Any]] | None = None,
):
    body: dict[str, Any] = {"errorCode": code, "message": message}
    if completeness is not None:
        body["dataCompletenessRate"] = completeness
    if details:
        body["details"] = details
    return jsonify(body), status


def require_api_key() -> tuple[Any, int] | None:
    configured_key = os.getenv("ML_API_KEY", "")
    if not configured_key:
        return None
    supplied_key = request.headers.get("X-Api-Key", "")
    if hmac.compare_digest(configured_key, supplied_key):
        return None
    return error_response("UNAUTHORIZED", "Geçerli bir ML API anahtarı gerekli.", 401)


def build_range_warnings(features: dict[str, Any]) -> list[str]:
    warnings: list[str] = []
    stats = FEATURE_CONFIG.get("numericFeatureStats", {})
    for name, limits in stats.items():
        value = features.get(name)
        if value is None:
            continue
        if value < limits["min"] or value > limits["max"]:
            warnings.append(f"{name} değeri eğitim aralığının dışında.")
    known_models = set(FEATURE_CONFIG.get("categories", {}).get("model", []))
    model_name = features.get("model")
    if model_name and model_name not in known_models:
        warnings.append("model değeri eğitim sırasında görülmemiş bir kategori.")
    return warnings


@app.before_request
def authenticate_request():
    if request.endpoint == "health":
        return None
    return require_api_key()


@app.get("/health")
def health():
    if MODEL is None:
        return jsonify({
            "status": "unhealthy",
            "errorCode": "MODEL_NOT_LOADED",
            "modelVersion": FEATURE_CONFIG.get("modelVersion", "unknown"),
        }), 503
    return jsonify({
        "status": "ok",
        "modelVersion": FEATURE_CONFIG.get("modelVersion", "unknown"),
    })


@app.get("/model-info")
@app.get("/api/v1/model-info")
def model_info():
    if not FEATURE_CONFIG:
        return error_response("MODEL_NOT_LOADED", "ML model bilgisi yüklenemedi.", 503)
    return jsonify({
        "modelVersion": FEATURE_CONFIG.get("modelVersion"),
        "features": FEATURE_CONFIG.get("features", []),
        "numericFeatureStats": FEATURE_CONFIG.get("numericFeatureStats", {}),
        "categories": FEATURE_CONFIG.get("categories", {}),
        "evaluationSummary": MODEL_METADATA.get("evaluationSummary", {}),
        "probabilityNotice": MODEL_METADATA.get("probabilityNotice"),
    })


@app.post("/predict")
@app.post("/api/v1/predict")
def predict():
    if MODEL is None:
        return error_response(
            "MODEL_NOT_LOADED",
            "ML modeli yüklenemedi; servis yöneticisi model_artifacts klasörünü kontrol etmelidir.",
            503,
        )
    payload = request.get_json(silent=True)
    if payload is None:
        return error_response("INVALID_JSON", "İstek gövdesi geçerli JSON değil.", 400)
    try:
        prediction_request = PredictionRequest.model_validate(payload)
    except ValidationError as exc:
        safe_details = [
            {"field": ".".join(str(part) for part in item["loc"]), "message": item["msg"]}
            for item in exc.errors(include_url=False, include_input=False)
        ]
        return error_response(
            "VALIDATION_ERROR", "Gönderilen özellikler geçerli değil.", 400, details=safe_details
        )

    features = prediction_request.features.model_dump()
    completeness, missing_fields = calculate_completeness(features)
    if completeness < 50.0:
        return error_response(
            "INSUFFICIENT_DATA",
            "Yapay zekâ tahmini için yeterli veri bulunmuyor.",
            422,
            completeness=completeness,
        )

    warnings = build_range_warnings(features)
    if missing_fields:
        warnings.append("Eksik özellikler eğitim medyanı veya 'Bilinmiyor' kategorisiyle işlendi.")
    model_row = {
        name: (
            "Bilinmiyor"
            if name in CATEGORICAL_FEATURES and features.get(name) is None
            else np.nan if features.get(name) is None else features[name]
        )
        for name in FEATURE_NAMES
    }

    try:
        frame = pd.DataFrame([model_row], columns=FEATURE_NAMES)
        probability = float(MODEL.predict_proba(frame)[0][1])
        ai_score = round(float(np.clip(probability, 0.0, 1.0)) * 100.0, 1)
        # Binary prediction certainty: confidence in either failure or no-failure class.
        model_confidence = round(max(probability, 1.0 - probability) * 100.0, 1)
        level = risk_level_from_score(ai_score)
        rule_explanation = build_rule_based_explanation(features)
        response = {
            "machineId": prediction_request.machineId,
            "aiRiskScore": ai_score,
            "failureProbability": round(probability, 4),
            "modelConfidence": model_confidence,
            "aiRiskLevel": level,
            "dataCompletenessRate": completeness,
            "missingFields": missing_fields,
            "warnings": warnings,
            "recommendedActions": build_recommended_actions(level, features),
            "ruleBasedExplanation": rule_explanation,
            "modelExplanation": build_model_explanation(features, MODEL_METADATA),
            # Kept as a compatibility alias; it is explicitly rule-based.
            "explanation": rule_explanation,
            "modelVersion": FEATURE_CONFIG.get("modelVersion", "unknown"),
            "predictionDate": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        }
        return jsonify(response)
    except Exception:
        LOGGER.exception("Tahmin üretilemedi. MachineId=%s", prediction_request.machineId)
        return error_response(
            "PREDICTION_ERROR", "Tahmin üretilirken beklenmeyen bir hata oluştu.", 500
        )


@app.errorhandler(404)
def not_found(_error):
    return error_response("NOT_FOUND", "İstenen adres bulunamadı.", 404)


@app.errorhandler(405)
def method_not_allowed(_error):
    return error_response("METHOD_NOT_ALLOWED", "HTTP metodu desteklenmiyor.", 405)


@app.errorhandler(413)
def request_too_large(_error):
    return error_response("REQUEST_TOO_LARGE", "İstek gövdesi boyut sınırını aşıyor.", 413)


load_resources()


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=int(os.getenv("ML_PORT", "5001")), debug=False)
