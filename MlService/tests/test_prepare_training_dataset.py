import json

import pandas as pd
import pytest

from prepare_training_dataset import build_dataset, load_mapping


def test_example_mapping_refuses_unverified_erp_columns(tmp_path):
    mapping = tmp_path / "mapping.json"
    mapping.write_text(json.dumps({"status": "TODO: ERP field mapping required"}), encoding="utf-8")
    with pytest.raises(ValueError, match="ERP field mapping required"):
        load_mapping(mapping)


def test_future_label_does_not_leak_into_historical_features():
    utc = "UTC"
    tables = {
        "machines": pd.DataFrame([{
            "machine_id": "M-1", "model": "model1", "criticality": "Orta",
            "installation_date": pd.Timestamp("2020-01-01", tz=utc),
        }]),
        "failures": pd.DataFrame([{
            "machine_id": "M-1", "occurred_at": pd.Timestamp("2026-01-10", tz=utc),
            "failure_type": "Rulman", "downtime_minutes": 30,
            "intervention_minutes": 20, "severity": "Orta",
        }]),
        "maintenance": pd.DataFrame(columns=["machine_id", "performed_at"]),
        "errors": pd.DataFrame(columns=["machine_id", "occurred_at"]),
        "telemetry": pd.DataFrame([
            telemetry("2026-01-01"), telemetry("2026-01-15")
        ]),
    }
    tables["maintenance"]["performed_at"] = pd.to_datetime(
        tables["maintenance"]["performed_at"], utc=True
    )
    tables["errors"]["occurred_at"] = pd.to_datetime(
        tables["errors"]["occurred_at"], utc=True
    )

    dataset = build_dataset(tables).reset_index(drop=True)

    assert dataset.loc[0, "ariza_sayisi_30g"] == 0
    assert dataset.loc[0, "gelecek_30g_ariza"] == 1
    assert dataset.loc[1, "ariza_sayisi_30g"] == 1
    assert dataset.loc[1, "gelecek_30g_ariza"] == 0


def telemetry(date):
    return {
        "machine_id": "M-1", "recorded_at": pd.Timestamp(date, tz="UTC"),
        "voltage": 170, "rotation": 440, "pressure": 100, "vibration": 40,
        "anomaly_score": 4, "production_intensity": 60,
    }
