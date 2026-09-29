"""Create leakage-safe training rows from explicitly mapped ERP CSV exports.

Real ERP column names are intentionally not guessed. Copy erp_mapping.example.json,
replace every TODO value, and pass the completed mapping with --mapping.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

import numpy as np
import pandas as pd

from model_utils import FEATURE_NAMES

BASE_DIR = Path(__file__).resolve().parent
DEFAULT_MAPPING_PATH = BASE_DIR / "erp_mapping.example.json"
DEFAULT_OUTPUT_PATH = BASE_DIR / "data" / "erp_ml_egitim_verisi.csv"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mapping", type=Path, default=DEFAULT_MAPPING_PATH)
    parser.add_argument("--input-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT_PATH)
    return parser.parse_args()


def load_mapping(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise FileNotFoundError(f"ERP mapping dosyası bulunamadı: {path.resolve()}")
    mapping = json.loads(path.read_text(encoding="utf-8"))
    serialized = json.dumps(mapping, ensure_ascii=False)
    if "TODO" in serialized:
        raise ValueError(
            "TODO: ERP field mapping required. erp_mapping.example.json dosyasını kopyalayıp "
            "gerçek ERP dosya ve kolon adlarıyla doldurun."
        )
    return mapping


def read_mapped_table(input_dir: Path, mapping: dict[str, Any], table: str) -> pd.DataFrame:
    relative_name = mapping["files"][table]
    path = (input_dir / relative_name).resolve()
    if not path.is_file():
        raise FileNotFoundError(f"{table} dışa aktarımı bulunamadı: {path}")
    frame = pd.read_csv(path)
    canonical_to_source = mapping["columns"][table]
    missing = sorted(set(canonical_to_source.values()).difference(frame.columns))
    if missing:
        raise ValueError(f"{table} dosyasında mapping kolonları eksik: {missing}")
    return frame.rename(columns={source: canonical for canonical, source in canonical_to_source.items()})[
        list(canonical_to_source)
    ]


def prepare_tables(input_dir: Path, mapping: dict[str, Any]) -> dict[str, pd.DataFrame]:
    tables = {
        name: read_mapped_table(input_dir, mapping, name)
        for name in ("machines", "failures", "maintenance", "errors", "telemetry")
    }
    for table, column in (
        ("machines", "installation_date"),
        ("failures", "occurred_at"),
        ("maintenance", "performed_at"),
        ("errors", "occurred_at"),
        ("telemetry", "recorded_at"),
    ):
        tables[table][column] = pd.to_datetime(tables[table][column], errors="raise", utc=True)
    numeric_columns = {
        "failures": ["downtime_minutes", "intervention_minutes"],
        "telemetry": [
            "voltage", "rotation", "pressure", "vibration",
            "anomaly_score", "production_intensity",
        ],
    }
    for table, columns in numeric_columns.items():
        for column in columns:
            tables[table][column] = pd.to_numeric(tables[table][column], errors="coerce")
    return tables


def window(frame: pd.DataFrame, date_column: str, at: pd.Timestamp, days: int) -> pd.DataFrame:
    return frame[(frame[date_column] <= at) & (frame[date_column] > at - pd.Timedelta(days=days))]


def build_row(machine: pd.Series, at: pd.Timestamp, tables: dict[str, pd.DataFrame]) -> dict[str, Any]:
    machine_id = machine["machine_id"]
    failures = tables["failures"][tables["failures"]["machine_id"] == machine_id]
    maintenance = tables["maintenance"][tables["maintenance"]["machine_id"] == machine_id]
    errors = tables["errors"][tables["errors"]["machine_id"] == machine_id]
    telemetry = tables["telemetry"][tables["telemetry"]["machine_id"] == machine_id]

    failures_30 = window(failures, "occurred_at", at, 30)
    failures_90 = window(failures, "occurred_at", at, 90)
    maintenance_30 = window(maintenance, "performed_at", at, 30)
    maintenance_90 = window(maintenance, "performed_at", at, 90)
    errors_7 = window(errors, "occurred_at", at, 7)
    errors_30 = window(errors, "occurred_at", at, 30)
    telemetry_7 = window(telemetry, "recorded_at", at, 7)
    historical_maintenance = maintenance[maintenance["performed_at"] <= at]
    last_maintenance = historical_maintenance["performed_at"].max()
    repeated_failures = failures_90.groupby("failure_type").size().sub(1).clip(lower=0).sum()
    future_failure = failures[
        (failures["occurred_at"] > at)
        & (failures["occurred_at"] <= at + pd.Timedelta(days=30))
    ]

    def average(column: str) -> float:
        return float(telemetry_7[column].mean()) if not telemetry_7.empty else np.nan

    installation_date = machine["installation_date"]
    row = {
        "anlik_tarih": at.isoformat(),
        "makine_id": machine_id,
        "yas": max(0, int((at - installation_date).days // 365.25)),
        "son_bakimdan_gecen_gun": (
            max(0, int((at - last_maintenance).days)) if pd.notna(last_maintenance) else np.nan
        ),
        "bakim_sayisi_30g": len(maintenance_30),
        "bakim_sayisi_90g": len(maintenance_90),
        "ariza_sayisi_30g": len(failures_30),
        "ariza_sayisi_90g": len(failures_90),
        "toplam_gecmis_ariza": int((failures["occurred_at"] <= at).sum()),
        "hata_sayisi_7g": len(errors_7),
        "hata_sayisi_30g": len(errors_30),
        "durus_suresi_30g": float(failures_30["downtime_minutes"].sum()),
        "ort_mudahale_suresi_90g": (
            float(failures_90["intervention_minutes"].mean()) if not failures_90.empty else np.nan
        ),
        "tekrar_ariza_sayisi_90g": int(repeated_failures),
        "volt_ort_7g": average("voltage"),
        "devir_ort_7g": average("rotation"),
        "basinc_ort_7g": average("pressure"),
        "titresim_ort_7g": average("vibration"),
        "anomali_puani_7g": average("anomaly_score"),
        "uretim_yogunlugu_7g": average("production_intensity"),
        "model": machine["model"],
        "kritik_seviye": machine["criticality"],
        "gelecek_30g_ariza": int(not future_failure.empty),
    }
    return row


def build_dataset(tables: dict[str, pd.DataFrame]) -> pd.DataFrame:
    rows: list[dict[str, Any]] = []
    for _, machine in tables["machines"].iterrows():
        machine_dates = (
            tables["telemetry"]
            .loc[tables["telemetry"]["machine_id"] == machine["machine_id"], "recorded_at"]
            .dt.floor("D")
            .drop_duplicates()
            .sort_values()
        )
        for calculation_date in machine_dates:
            rows.append(build_row(machine, calculation_date, tables))
    if not rows:
        raise ValueError("Mapping uygulandı ancak eğitim satırı üretilemedi.")
    columns = ["anlik_tarih", "makine_id", *FEATURE_NAMES, "gelecek_30g_ariza"]
    return pd.DataFrame(rows, columns=columns).sort_values(["anlik_tarih", "makine_id"])


def main() -> None:
    args = parse_args()
    mapping = load_mapping(args.mapping.resolve())
    tables = prepare_tables(args.input_dir.resolve(), mapping)
    dataset = build_dataset(tables)
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    dataset.to_csv(output, index=False)
    print(f"{len(dataset)} sızıntısız eğitim satırı yazıldı: {output}")
    print("Not: Bu çıktı ancak mapping gerçek ERP ekibi tarafından doğrulandıktan sonra kullanılmalıdır.")


if __name__ == "__main__":
    main()
