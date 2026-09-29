# Frontend Entegrasyon Rehberi

## Bağlantı

Frontend template artık `BackendMaintenanceDataService` ile backend'e bağlanır:

```json
"Backend": {
  "BaseUrl": "http://localhost:5224/",
  "UseMockData": false
}
```

`UseMockData=true` yalnızca Development'ta kullanılabilir. Backend CORS origin listesi
`Cors:AllowedOrigins` veya `Cors__AllowedOrigins__0` biçiminde verilir.

## JSON ve tarih

- Endpoint tabanı: `/api/v1`
- JSON: camelCase, enumlar okunabilir string.
- Tarihler: ISO-8601 UTC (`2026-07-30T19:00:00Z`); kullanıcı saat dilimi frontend'de çevrilir.
- Para/skor/oranlar JSON number ve backend'de `decimal`.
- `null`, veri yok demektir; özellikle başarısız ML sonucu `machineLearningScore=null` olur.

## Pagination, filtre ve sıralama

Ortak query alanları `page`, `pageSize` (en fazla 200); response:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0,
  "hasPrevious": false,
  "hasNext": false
}
```

Makine filtreleri: `search`, `companyId`, `branchId`, `departmentId`, `productionLineId`, `status`,
`riskLevel`, `sortBy`, `descending`.

Arıza filtreleri: `machineId`, `failureType`, `severity`, `status`, `from`, `to`.

## Risk gösterimi

Frontend eşik hesaplamaz; backend'in `risk` nesnesini kullanır:

```json
{
  "score": 72.4,
  "level": "Yüksek",
  "riskColorKey": "red",
  "colorHex": "#EF4444"
}
```

Anahtarlar: `green`, `amber`, `red`, `dark-red`. Kesin sınırlar sırasıyla
`[0,30)`, `[30,60)`, `[60,80)`, `[80,100]`.

## Hata cevapları

Validation/business/harici servis hataları RFC 7807 `application/problem+json` döner:

```json
{
  "type": "about:blank",
  "title": "Çakışma",
  "status": 409,
  "detail": "Durum geçişi geçersiz.",
  "instance": "/api/v1/...",
  "traceId": "..."
}
```

Her istekte opsiyonel `X-Correlation-ID` gönderilebilir; aynı header response'a yazılır.

## Frontend'in kullandığı endpointler

- `GET /api/v1/dashboard?months=6`
- `GET /api/v1/bakim-yonetimi/machines?page=1&pageSize=200`
- `GET /api/v1/bakim-yonetimi/machines/{id}`
- `GET /api/v1/bakim-yonetimi/failures?machineId={id}&page=1&pageSize=200`
- `GET /api/v1/bakim-yonetimi/maintenance/plans?page=1&pageSize=200`

Bu dört salt-okunur endpoint teslim edilen merkezi SQL Server şemasını kullanır. Mevcut standart
`/api/v1/machines`, `/failures` ve `/maintenance` endpointleri komut/iş akışı API'leri olarak korunur.

Yazma ve iş akışı endpointleri için `BACKEND_README.md` ve Swagger kullanılmalıdır.

## Authentication

Kullanıcı kimlik sistemi henüz dış ekip sözleşmesine bağlıdır ve gereksiz ASP.NET Identity
kurulmamıştır. ERP/ML callback endpointleri `X-Integration-Key` ister. Browser/frontend bu anahtarı
tutmamalı veya entegrasyon endpointlerine doğrudan göndermemelidir.

## Geçici test panelini kapatma

`/dev-test` controller'ı yalnızca Development'ta cevap verir. Production'da 404'tür. Gerçek frontend
hazır olduğunda dosyaların silinmesi gerekmez; environment koruması API sözleşmesini etkilemeden paneli
devre dışı tutar.
