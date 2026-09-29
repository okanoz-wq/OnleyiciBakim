# API Sözleşmesi ve Örnekler

Base URL: `/api/v1`

## Frontend ekran eşlemesi

| Ekran | Endpoint |
|---|---|
| Dashboard kartları, grafikler, en riskli 5 makine | `GET /dashboard?months=6&riskyMachineCount=5` |
| Makine listesi | `GET /machines?page=1&pageSize=20&sortBy=riskScore` |
| Makine sağlık detayı | `GET /machines/{id}` |
| Arıza geçmişi ve filtreler | `GET /failures?machineId=&failureType=&severity=&status=` |
| Bakım planı kartları | `GET /maintenance/plans` |
| Bakım takvimi | `GET /maintenance/calendar?from=&to=` |
| Erken uyarılar | `GET /alerts?status=Open` |
| Model-kural karşılaştırması | `GET /reports/model-rule-comparison` |

Liste endpointleri `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage` alanlarını döndürür.

## Risk önizleme

`POST /api/v1/risks/preview`

```json
{
  "machineLearningScore": 82.4,
  "algorithmicInput": {
    "daysSinceLastMaintenance": 95,
    "failureCount30Days": 2,
    "errorCount7Days": 3,
    "anomalyScore7Days": 10.4,
    "productionIntensity7Days": 88.2,
    "criticality": "Kritik"
  }
}
```

Bu endpoint kayıt açmadan formülü test eder. Kalıcı makine değerlendirmesi:

`POST /api/v1/risks/machines/MAK-0083/evaluate`

```json
{
  "machineLearningScore": 82.4,
  "modelConfidence": 91.2,
  "modelName": "predictive-maintenance",
  "modelVersion": "1.0.0",
  "correlationId": "ml-2026-07-30-MAK-0083",
  "machineLearningReasons": [
    "7 gün içinde rulman arızası olasılığı yükseldi",
    "Titreşim eğiliminde sapma"
  ]
}
```

Backend algoritmik özellikleri son bakım, arıza, hata ve telemetri tablolarından kendisi toplar. ML ekibi yalnızca 0–100 skorunu, model bilgisini ve açıklamalarını gönderir.

Örnek risk cevabında frontend doğrudan kullanabilsin diye seviye ve renk birlikte döner:

```json
{
  "algorithmicScore": 65.27,
  "machineLearningScore": 82.4,
  "algorithmWeight": 0.30,
  "machineLearningWeight": 0.70,
  "hybridRisk": {
    "score": 77.26,
    "level": "Yüksek",
    "color": "kırmızı",
    "colorHex": "#EF4444"
  }
}
```

## ML callback

`POST /api/v1/integrations/ml/predictions`

Header:

```text
X-Integration-Key: <secret>
```

```json
{
  "machineId": "MAK-0083",
  "machineLearningScore": 82.4,
  "modelConfidence": 91.2,
  "modelName": "predictive-maintenance",
  "modelVersion": "1.0.0",
  "correlationId": "ml-2026-07-30-MAK-0083",
  "machineLearningReasons": ["Titreşim sapması"]
}
```

`correlationId` tekrar gönderilirse aynı risk değerlendirmesi döner; yeni uyarı veya plan açılmaz.

## ERP makine senkronizasyonu

`POST /api/v1/integrations/erp/machines`

```json
{
  "correlationId": "erp-machine-batch-20260730-001",
  "machines": [
    {
      "id": "MAK-0001",
      "externalId": "ERP-12345",
      "code": "M0001",
      "name": "Ekstrüzyon Makinesi 001",
      "companyId": "F001",
      "branchId": "SUB-01",
      "departmentId": "DEP-01-01",
      "productionLineId": "HAT-01-01",
      "type": "Ekstrüzyon",
      "model": "model3",
      "age": 18,
      "criticality": "Yüksek",
      "installationYear": 2008,
      "status": "Aktif",
      "dataSource": "ERP"
    }
  ]
}
```

ERP arızaları için aynı yapı `POST /api/v1/integrations/erp/failures` endpointinde kullanılır. Senkronizasyon sonucu oluşturulan/güncellenen/reddedilen sayıları ve satır hatalarını verir.

## Bakım gerçekleştirme

`POST /api/v1/maintenance/plans/PLN-000001/complete`

```json
{
  "maintenanceRecordId": "BAK-004501",
  "performedAt": "2026-07-30T10:00:00Z",
  "personnelId": "PER-004",
  "durationMinutes": 90,
  "result": "Rulman kontrol edildi ve yağlama tamamlandı",
  "cost": 2450.00,
  "nextMaintenanceAt": "2026-10-30T10:00:00Z",
  "checklistResults": {
    "Koruyucu ekipman kontrolü": "Uygun",
    "Titreşim ölçümü": "3.2 mm/s"
  }
}
```

## Hata biçimi

Tüm iş kuralı ve sunucu hataları `application/problem+json` döner:

```json
{
  "type": "about:blank",
  "title": "Kaynak bulunamadı",
  "status": 404,
  "detail": "'MAK-9999' kimlikli makine bulunamadı.",
  "instance": "/api/v1/machines/MAK-9999",
  "traceId": "..."
}
```
