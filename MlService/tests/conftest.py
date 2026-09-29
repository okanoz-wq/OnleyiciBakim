from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
import pytest

ML_ROOT = Path(__file__).resolve().parents[1]
if str(ML_ROOT) not in sys.path:
    sys.path.insert(0, str(ML_ROOT))

import app as ml_app  # noqa: E402
from model_utils import FEATURE_NAMES, NUMERIC_FEATURES  # noqa: E402


class DummyModel:
    def predict_proba(self, frame):
        return np.array([[0.316, 0.684] for _ in range(len(frame))])


@pytest.fixture()
def valid_payload():
    return {
        "machineId": "CNC-001",
        "features": {
            "yas": 12,
            "son_bakimdan_gecen_gun": 8,
            "bakim_sayisi_30g": 1,
            "bakim_sayisi_90g": 3,
            "ariza_sayisi_30g": 0,
            "ariza_sayisi_90g": 1,
            "toplam_gecmis_ariza": 4,
            "hata_sayisi_7g": 1,
            "hata_sayisi_30g": 3,
            "durus_suresi_30g": 45,
            "ort_mudahale_suresi_90g": 38.5,
            "tekrar_ariza_sayisi_90g": 0,
            "volt_ort_7g": 170.2,
            "devir_ort_7g": 445.6,
            "basinc_ort_7g": 98.7,
            "titresim_ort_7g": 41.1,
            "anomali_puani_7g": 4.1,
            "uretim_yogunlugu_7g": 55.0,
            "model": "model3",
            "kritik_seviye": "Yüksek",
        },
    }


@pytest.fixture()
def client(monkeypatch):
    monkeypatch.setattr(ml_app, "MODEL", DummyModel())
    monkeypatch.setattr(ml_app, "RESOURCE_ERROR", None)
    monkeypatch.setattr(ml_app, "FEATURE_CONFIG", {
        "modelVersion": "test-v1",
        "features": [{"name": name} for name in FEATURE_NAMES],
        "numericFeatureStats": {
            name: {"min": 0, "max": 1000, "mean": 10, "std": 1, "median": 10}
            for name in NUMERIC_FEATURES
        },
        "categories": {
            "model": ["model1", "model2", "model3", "model4"],
            "kritik_seviye": ["Düşük", "Orta", "Yüksek", "Kritik"],
        },
    })
    monkeypatch.setattr(ml_app, "MODEL_METADATA", {
        "featureImportance": [{"feature": "anomali_puani_7g", "importance": 0.25}],
        "evaluationSummary": {},
        "probabilityNotice": "test",
    })
    ml_app.app.config.update(TESTING=True)
    return ml_app.app.test_client()
