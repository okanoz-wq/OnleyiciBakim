from pathlib import Path

import app as ml_app


def test_health_is_successful(client):
    response = client.get("/health")
    assert response.status_code == 200
    assert response.get_json()["status"] == "ok"


def test_model_info_is_successful(client):
    response = client.get("/model-info")
    assert response.status_code == 200
    assert len(response.get_json()["features"]) == 20


def test_predict_valid_input(client, valid_payload):
    response = client.post("/api/v1/predict", json=valid_payload)
    assert response.status_code == 200
    body = response.get_json()
    assert 0 <= body["aiRiskScore"] <= 100
    assert body["aiRiskLevel"] == "Yüksek"
    assert body["dataCompletenessRate"] == 100
    assert 50 <= body["modelConfidence"] <= 100
    assert body["modelConfidence"] == round(
        max(body["failureProbability"], 1 - body["failureProbability"]) * 100, 1
    )
    assert body["missingFields"] == []
    assert "recommendedMaintenanceDate" not in body


def test_null_field_is_imputed_and_reported(client, valid_payload):
    valid_payload["features"]["volt_ort_7g"] = None
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 200
    body = response.get_json()
    assert body["dataCompletenessRate"] == 95
    assert body["missingFields"] == ["volt_ort_7g"]


def test_missing_critical_category_is_imputed(client, valid_payload):
    valid_payload["features"].pop("kritik_seviye")
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 200
    assert "kritik_seviye" in response.get_json()["missingFields"]


def test_less_than_fifty_percent_is_rejected(client, valid_payload):
    keys = list(valid_payload["features"])
    for key in keys[:11]:
        valid_payload["features"][key] = None
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 422
    assert response.get_json()["errorCode"] == "INSUFFICIENT_DATA"
    assert response.get_json()["dataCompletenessRate"] == 45


def test_negative_value_is_rejected(client, valid_payload):
    valid_payload["features"]["yas"] = -1
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 400
    assert response.get_json()["errorCode"] == "VALIDATION_ERROR"


def test_invalid_criticality_is_rejected(client, valid_payload):
    valid_payload["features"]["kritik_seviye"] = "Çok Kritik"
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 400


def test_boolean_is_not_accepted_as_number(client, valid_payload):
    valid_payload["features"]["yas"] = True
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 400
    assert "bool" in response.get_json()["details"][0]["message"]


def test_unknown_model_returns_warning(client, valid_payload):
    valid_payload["features"]["model"] = "never-seen-model"
    response = client.post("/predict", json=valid_payload)
    assert response.status_code == 200
    assert any("görülmemiş" in warning for warning in response.get_json()["warnings"])


def test_optional_api_key_protects_prediction_but_not_health(
    client, valid_payload, monkeypatch
):
    monkeypatch.setenv("ML_API_KEY", "secret")
    assert client.get("/health").status_code == 200
    assert client.post("/predict", json=valid_payload).status_code == 401
    response = client.post(
        "/predict", json=valid_payload, headers={"X-Api-Key": "secret"}
    )
    assert response.status_code == 200


def test_missing_model_file_has_safe_health_error(client, tmp_path: Path):
    ml_app.load_resources(
        tmp_path / "missing-model.pkl",
        tmp_path / "missing-config.json",
        tmp_path / "missing-metadata.json",
    )
    response = client.get("/health")
    assert response.status_code == 503
    body = response.get_json()
    assert body["errorCode"] == "MODEL_NOT_LOADED"
    assert "missing-model.pkl" not in str(body)


def test_real_artifacts_load_and_predict(valid_payload):
    ml_app.load_resources()
    assert ml_app.MODEL is not None
    ml_app.app.config.update(TESTING=True)
    with ml_app.app.test_client() as real_client:
        response = real_client.post("/api/v1/predict", json=valid_payload)
    assert response.status_code == 200
    body = response.get_json()
    assert 0 <= body["aiRiskScore"] <= 100
    assert body["modelVersion"] == "rf-v2-time-aware"
