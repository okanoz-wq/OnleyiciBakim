"""Shared risk labels, data-completeness rules, actions and explanations."""

from __future__ import annotations

from typing import Any


NUMERIC_FEATURES = [
    "yas",
    "son_bakimdan_gecen_gun",
    "bakim_sayisi_30g",
    "bakim_sayisi_90g",
    "ariza_sayisi_30g",
    "ariza_sayisi_90g",
    "toplam_gecmis_ariza",
    "hata_sayisi_7g",
    "hata_sayisi_30g",
    "durus_suresi_30g",
    "ort_mudahale_suresi_90g",
    "tekrar_ariza_sayisi_90g",
    "volt_ort_7g",
    "devir_ort_7g",
    "basinc_ort_7g",
    "titresim_ort_7g",
    "anomali_puani_7g",
    "uretim_yogunlugu_7g",
]
CATEGORICAL_FEATURES = ["model", "kritik_seviye"]
FEATURE_NAMES = NUMERIC_FEATURES + CATEGORICAL_FEATURES

ACTION_POLICY = {
    "Düşük": "Normal periyodik bakım planına devam edin.",
    "Orta": "21–30 gün içinde kontrol planlayın.",
    "Yüksek": "7 gün içinde bakım planlayın.",
    "Kritik": "1–3 gün içinde acil bakım değerlendirmesi yapın.",
}


def risk_level_from_score(score: float) -> str:
    """Classify a bounded 0-100 score without decimal gaps at boundaries."""
    score = max(0.0, min(100.0, float(score)))
    if score >= 80:
        return "Kritik"
    if score >= 60:
        return "Yüksek"
    if score >= 30:
        return "Orta"
    return "Düşük"


def calculate_completeness(features: dict[str, Any]) -> tuple[float, list[str]]:
    """Return filled-feature percentage; machineId is intentionally excluded."""
    missing = [
        name
        for name in FEATURE_NAMES
        if features.get(name) is None
        or (isinstance(features.get(name), str) and not features[name].strip())
    ]
    rate = (len(FEATURE_NAMES) - len(missing)) / len(FEATURE_NAMES) * 100.0
    return round(rate, 2), missing


def build_recommended_actions(risk_level: str, features: dict[str, Any]) -> list[str]:
    """Produce non-binding action windows; the backend chooses the actual date."""
    actions = [ACTION_POLICY[risk_level]]
    failures_30 = features.get("ariza_sayisi_30g") or 0
    repeated_90 = features.get("tekrar_ariza_sayisi_90g") or 0
    anomaly = features.get("anomali_puani_7g") or 0
    if failures_30 >= 2 or repeated_90 >= 1:
        actions.append("Tekrarlayan arıza örüntüsü için kök neden analizi yapın.")
    if anomaly >= 8:
        actions.append("Telemetri ve titreşim takibini sıklaştırın.")
    return actions


def build_rule_based_explanation(features: dict[str, Any]) -> list[str]:
    """Describe input observations; these are not presented as model explanations."""
    explanations: list[str] = []
    if features.get("ariza_sayisi_30g") is not None:
        explanations.append(
            f"Son 30 günde {features['ariza_sayisi_30g']:g} arıza kaydı bulundu."
        )
    if features.get("son_bakimdan_gecen_gun") is not None:
        explanations.append(
            f"Son bakımdan bu yana {features['son_bakimdan_gecen_gun']:g} gün geçti."
        )
    if features.get("anomali_puani_7g") is not None:
        explanations.append(
            f"Yedi günlük ortalama anomali puanı {features['anomali_puani_7g']:g}."
        )
    criticality = features.get("kritik_seviye")
    if criticality:
        explanations.append(f"Makine kritiklik seviyesi {criticality}.")
    return explanations


def build_model_explanation(
    features: dict[str, Any], metadata: dict[str, Any]
) -> list[dict[str, Any]]:
    """Expose global RF importance context, explicitly not a local SHAP explanation."""
    importances = metadata.get("featureImportance", [])[:5]
    return [
        {
            "feature": item["feature"],
            "value": features.get(item["feature"]),
            "globalImportance": item["importance"],
            "method": "global_random_forest_importance",
        }
        for item in importances
    ]
