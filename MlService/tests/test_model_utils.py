import pytest

from model_utils import risk_level_from_score


@pytest.mark.parametrize(
    ("score", "expected"),
    [
        (0, "Düşük"),
        (29, "Düşük"),
        (29.99, "Düşük"),
        (30, "Orta"),
        (59.99, "Orta"),
        (60, "Yüksek"),
        (79.99, "Yüksek"),
        (80, "Kritik"),
        (100, "Kritik"),
    ],
)
def test_risk_boundaries(score, expected):
    assert risk_level_from_score(score) == expected


def test_risk_score_is_clamped():
    assert risk_level_from_score(-1) == "Düşük"
    assert risk_level_from_score(101) == "Kritik"
