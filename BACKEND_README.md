# Önleyici Bakım Backend Teslim Rehberi

## 1. Mimari ve teknoloji

Çözüm .NET 8 üzerinde üç projeden oluşur:

- `OnleyiciBakim`: ASP.NET Core MVC + REST API, EF Core 8, Npgsql, Swagger.
- `OnleyiciBakim.Tests`: xUnit unit ve `WebApplicationFactory` integration testleri.
- `Frontend`: frontend ekibinden gelen MVC template; mock yerine backend API istemcisini kullanır.

Backend; `Controllers`, `Contracts`, `Domain`, `Data`, `Services`, `Integrations`, `Options`,
`Infrastructure`, `BackgroundJobs` ve `ViewModels` katmanlarına ayrılmıştır. API entity döndürmez;
request/response contract veya projection kullanır. Liste sorguları sayfalı ve `AsNoTracking` çalışır.

## 2. Veritabanı ve migration durumu

Teslim edilen `BakimYonetimiDb` SQL Server yedeğinin 33 iş tablosu `Data/BakimYonetimi` altında
birebir modellenmiştir. Dashboard ile MVC makine/arıza/plan okumaları bu salt-okunur bağlamı kullanır;
kaynak şemada uygulama tarafından DDL veya migration çalıştırılmaz. Development bağlantısı
`ConnectionStrings:BakimYonetimiSqlServer` ile yapılandırılır.

Komut modeli için ana hedef PostgreSQL/Npgsql'dir. Mapping, indeks, decimal precision, enum conversion, unique constraint
ve `snake_case` adlandırma `ApplicationDbContext` içinde merkezidir. Uygulama:

- `Database.EnsureCreated()` çağırmaz.
- Başlangıçta migration çalıştırmaz.
- Veritabanı silmez.
- Repository içinde migration taşımaz; nihai şema veritabanı ekibinden beklendiği için mapping teslim
  edilen şemaya göre uyarlanmalıdır.

Production bağlantısı secret/environment variable ile verilmelidir:

```powershell
$env:ConnectionStrings__PostgreSql="Host=...;Database=...;Username=...;Password=..."
$env:Database__Provider="PostgreSql"
```

Development varsayılanı `InMemory` ve idempotent `DevelopmentDataSeeder`'dır. Bu sağlayıcı Production'da
seçilirse uygulama başlamaz.

## 3. Risk motoru

Kesin sınıflandırma tek merkezde `RiskClassificationService` tarafından uygulanır:

| Skor | Seviye | `riskColorKey` | Hex |
|---|---|---|---|
| `0 <= x < 30` | Düşük | `green` | `#16A34A` |
| `30 <= x < 60` | Orta | `amber` | `#F59E0B` |
| `60 <= x < 80` | Yüksek | `red` | `#EF4444` |
| `80 <= x <= 100` | Kritik | `dark-red` | `#7F1D1D` |

Ham değerler 0-100'e clamp edilir. Hibrit skor hesap sırasında yuvarlanmaz ve 20 model
özelliğinin veri tamlığına göre ağırlık seçer:

```text
tamlık >= %80  -> AI × 0.40 + kural × 0.60
tamlık %50–79  -> AI × 0.20 + kural × 0.80
tamlık < %50   -> kural × 1.00
```

Algoritmik faktörler `AlgorithmicRisk` config bölümünden gelir ve toplamları başlangıçta 1.00 olarak
doğrulanır:

- Arıza sıklığı `%25`
- Son bakımdan geçen süre `%20`
- Duruş süresi `%15`
- Tekrarlayan arıza `%15`
- Telemetri/anomali `%15`
- Makine kritiklik `%10`

Her analiz ham değer, normalize değer, ağırlık, katkı, açıklama, eksik faktörler ve
`dataCompletenessPercent` üretir. Kritik telemetri ve 7 günlük kritik arıza tekrarı skoru en az
80'e; geçmiş zorunlu bakım tarihi en az 60'a taşır. Bakım planı nihai seviyeden 60/21/7/3 gün
ile hesaplanır; aktif plan varsa kopya oluşturmak yerine güncellenir.

## 4. ML entegrasyonu

`IMachineFeatureService`, uygulama tablolarından (ve mevcut SQL Server şeması seçiliyse onun gerçek
alanlarından) 20 snake_case özelliği aggregate sorgularla üretir. `IMlPredictionClient` bu DTO'yu
Python 3.11 servisine yollar. Testler için `MockMlPredictionClient` korunur. Ayarlar:

```json
"MlService": {
  "BaseUrl": "http://127.0.0.1:5001",
  "PredictionEndpoint": "/api/v1/predict",
  "TimeoutSeconds": 15,
  "ApiKey": "",
  "RetryCount": 2,
  "UseMock": false
}
```

Request `machineId` ve 20 alanlı `features` nesnesini taşır. Response `aiRiskScore`,
`failureProbability`, `aiRiskLevel`, `dataCompletenessRate`, `missingFields`, `warnings`, aksiyon,
açıklama, model sürümü ve tahmin tarihini destekler. Python kesin bakım tarihi üretmez.

Timeout, bağlantı, HTTP hata, bozuk JSON, geçersiz skor veya `%50` altı veri durumunda backend
kural skoruyla devam eder; `isFallback=true`, AI ağırlığı sıfır olur ve karar geçmişine açıkça
fallback kaydı yazılır. Ayrıntılı model sözleşmesi ve performans raporu `MlService/README.md` ile
`MlService/model_artifacts/evaluation_report.json` içindedir.

## 5. ERP ve outbox

`IErpClient` için HTTP ve Development/Test mock implementasyonları vardır. ERP'den makine çekme ve
bakım planından iş emri açma desteklenir. `maintenancePlanId` ve `Idempotency-Key` eşlemesi tekrar iş
emri açılmasını önler. ERP erişilemiyorsa local `OutboxMessage` yazılır; `Erp:EnableBackgroundSync=true`
ise kontrollü exponential retry yapan worker işler ve beş başarısız denemede dead-letter'a taşır.

API'ye ERP/ML callback gönderen entegrasyon endpointleri `X-Integration-Key` ile korunur. Anahtar
secret store üzerinden `Integrations__ApiKey` olarak verilmelidir. Development'ta değer boşsa yerel
mock testine izin verilir; Production'da boş anahtar erişim sağlamaz.

## 6. Entity kapsamı

Organizasyon: `Company`, `Branch`, `Department`, `ProductionLine`, `Personnel`.

Makine/veri: `Machine`, `MachineComponent`, `DailyTelemetry`.

Arıza/bakım: `FailureRecord`, `FailureIntervention`, `MaintenanceRecord`, `MaintenancePlan`,
`MaintenancePlanTask`, `MaintenanceExecution`, `MaintenanceExecutionChecklistResult`, `ReplacedPart`.

Risk: `RiskAssessment`, `RiskFactorResultEntity`, `MlPredictionRecord`, `Alert`,
`DecisionHistory`.

Tanım/sistem: arıza kodu, bakım türü, checklist, parametre, `IntegrationSyncLog`,
`ErpWorkOrderMapping`, `OutboxMessage`, `AuditLog`.

## 7. Başlıca endpointler

| Alan | Endpoint |
|---|---|
| Health | `GET /health`, `/health/live`, `/health/ready` |
| Dashboard | `GET /api/v1/dashboard` |
| Makineler | `GET/POST /api/v1/machines`, `GET/PUT/DELETE /api/v1/machines/{id}` |
| Telemetri | `POST /api/v1/machines/{id}/telemetry` |
| Arızalar | `GET/POST /api/v1/failures`, `GET /{id}`, `POST /{id}/interventions`, `POST /{id}/close` |
| Bakım planı | `GET/POST /api/v1/maintenance/plans`, `POST /plans/from-analysis/{id}`, `POST /plans/{id}/approve` |
| Bakım yürütme | `POST /api/v1/maintenance-executions/plans/{id}/start`, `POST /{executionId}/complete` |
| Risk | `POST /api/v1/risks/preview`, `POST /api/v1/risk-analyses/machines/{id}`, `POST /bulk`, `GET /{id}` |
| Uyarı | `GET /api/v1/alerts`, `POST /{id}/assign`, `/create-maintenance-plan`, `/no-action`, `/resolve` |
| Karar geçmişi | `GET /api/v1/decision-history` |
| Rapor | `GET /api/v1/reports/failure-analysis`, `/maintenance-performance`, `/model-rule-comparison` |
| ERP | `POST /api/v1/integrations/erp/pull-machines`, `/erp/work-orders/{planId}`, `GET /erp/integration-logs` |
| Tanımlar | `/api/v1/definitions/*`, `/api/v1/reference-data/*` |

Tam örnekler `OnleyiciBakim.http`, `OnleyiciBakim.postman_collection.json` ve
`docs/API_ORNEKLERI.md` içindedir.

## 8. Swagger, test paneli ve seed

Development:

- Swagger: `/swagger`
- Backend test paneli: `/dev-test` ve `/dev-test/{machines|failures|risk-analysis|maintenance|integrations|logs}`
- Seed: dört farklı risk seviyesinde makine, açık/kapalı arıza, telemetri ve bakım planları.
- Mock ML senaryoları: `success`, `timeout`, `unavailable`, `invalid-score`.
- Mock ERP: makine çekme ve idempotent iş emri sonucu.

Production'da Swagger config ile açılmadıkça kapalıdır; `/dev-test` 404 döner; mock seçimi uygulamanın
başlamasını engeller; seed çalışmaz.

## 9. Çalıştırma ve test

```powershell
dotnet restore .\OnleyiciBakim.sln
dotnet build .\OnleyiciBakim.sln
dotnet test .\OnleyiciBakim.sln
dotnet run --project .\OnleyiciBakim\OnleyiciBakim.csproj
dotnet run --project .\Frontend\OnleyiciBakimSistemi.csproj
```

Development backend URL'i launch profile ile `http://localhost:5224`; frontend bu adresi
`Frontend/appsettings.Development.json` içinden kullanır.

## 10. CSV durumu

Repository'de CSV bulunmamıştır; bu nedenle kolon uydurulmamıştır. `ICsvImportService`, dosyayı
belleğe bütünüyle almadan satır satır okur, BOM/encoding, delimiter, quoted değer, duplicate header ve
kolon sayısı hatalarını yönetir. Gerçek veri sözlüğü geldiğinde import DTO/mapping eklenmelidir.

## 11. Dış ekiplerden beklenen kesin bilgiler

Veritabanı ekibi: nihai tablo/sütun adları, PK tipleri, enum saklama biçimi, ilişkiler, unique/check
constraintler, concurrency alanı ve uygulanmış migration/DDL sürümü.

ML ekibi: kesin endpoint, authentication header, feature ad/tip/null kuralları, response/error şeması,
confidence ölçeği, model versiyon politikası, SLA/timeout ve idempotency davranışı.

ERP ekibi: provider ve endpointler, authentication, ekipman/organizasyon/sayaç payloadları, iş emri
durum sözlüğü, stok hareketi sözleşmesi, hata kodları, rate limit ve idempotency desteği.

Bu bilgiler dış bağımlılıktır; backend adapter ve mapping noktaları hazırdır.
