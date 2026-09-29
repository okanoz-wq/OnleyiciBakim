"""Strict Pydantic request schema for the ML HTTP boundary."""

from __future__ import annotations

from typing import Any, Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator

from model_utils import NUMERIC_FEATURES


class MachineFeatures(BaseModel):
    model_config = ConfigDict(extra="forbid")

    yas: float | None = Field(default=None, ge=0)
    son_bakimdan_gecen_gun: float | None = Field(default=None, ge=0)
    bakim_sayisi_30g: float | None = Field(default=None, ge=0)
    bakim_sayisi_90g: float | None = Field(default=None, ge=0)
    ariza_sayisi_30g: float | None = Field(default=None, ge=0)
    ariza_sayisi_90g: float | None = Field(default=None, ge=0)
    toplam_gecmis_ariza: float | None = Field(default=None, ge=0)
    hata_sayisi_7g: float | None = Field(default=None, ge=0)
    hata_sayisi_30g: float | None = Field(default=None, ge=0)
    durus_suresi_30g: float | None = Field(default=None, ge=0)
    ort_mudahale_suresi_90g: float | None = Field(default=None, ge=0)
    tekrar_ariza_sayisi_90g: float | None = Field(default=None, ge=0)
    volt_ort_7g: float | None = Field(default=None, ge=0)
    devir_ort_7g: float | None = Field(default=None, ge=0)
    basinc_ort_7g: float | None = Field(default=None, ge=0)
    titresim_ort_7g: float | None = Field(default=None, ge=0)
    anomali_puani_7g: float | None = Field(default=None, ge=0, le=100)
    uretim_yogunlugu_7g: float | None = Field(default=None, ge=0, le=100)
    model: str | None = Field(default=None, min_length=1, max_length=100)
    kritik_seviye: Literal["Düşük", "Orta", "Yüksek", "Kritik"] | None = None

    @field_validator(*NUMERIC_FEATURES, mode="before")
    @classmethod
    def reject_boolean_numbers(cls, value: Any) -> Any:
        if isinstance(value, bool):
            raise ValueError("bool değeri sayısal özellik olarak kabul edilmez")
        return value

    @field_validator("model", mode="before")
    @classmethod
    def normalize_model(cls, value: Any) -> Any:
        if isinstance(value, str):
            value = value.strip()
        return value


class PredictionRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    machineId: str = Field(min_length=1, max_length=100)
    features: MachineFeatures

    @field_validator("machineId", mode="before")
    @classmethod
    def normalize_machine_id(cls, value: Any) -> Any:
        if isinstance(value, str):
            value = value.strip()
        return value
