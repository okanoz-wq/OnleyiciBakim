# BakimYonetimiDb Entegrasyon Notu

## Güncel durum (Ağustos 2026)

`BakimYonetimiDb_sema.sql`, en güncel `.bak` ve 11 sayfalık şema PDF'i birlikte doğrulandı.
Yedekte 33 iş tablosu vardır: 14 kaynak tablo ve ekran/risk/iş akışı ihtiyaçları için sonradan
eklenen 19 tablo. EF Core karşılıklarının tamamı `Data/BakimYonetimi` altında birebir scaffold
edilmiştir. Kaynak yedek merkezi referans olduğu için uygulama bu şemada migration,
`EnsureCreated` veya otomatik DDL çalıştırmaz.

Yedekteki doğrulanmış ana hacimler: 100 makine, 761 arıza, 3.286 bakım, 3.919 hata,
36.600 günlük telemetri, 100 güncel risk, 46 uyarı ve 100 bakım planıdır. Müdahale, iş emri,
risk analiz geçmişi/faktörleri, karar geçmişi ve parça hareketi tabloları sonraki uygulama/motor
işlemleriyle dolmak üzere boş bırakılmıştır.

## Kaynak veri analizi

ZIP paketindeki dosyalar ve karşılık gelen backend tabloları:

| CSV | EF entity / DbSet |
|---|---|
| `01_firmalar.csv` | `Company / Companies` |
| `02_subeler.csv` | `Branch / Branches` |
| `03_departmanlar.csv` | `Department / Departments` |
| `04_uretim_hatlari.csv` | `ProductionLine / ProductionLines` |
| `05_personeller.csv` | `Personnel` |
| `06_makineler.csv` | `Machine / Machines` |
| `07_ariza_kartlari.csv` | `FailureRecord / FailureRecords` |
| `08_bakim_kayitlari.csv` | `MaintenanceRecord / MaintenanceRecords` |
| `09_hata_kayitlari.csv` | `ErrorRecord / ErrorRecords` |
| `10_telemetri_gunluk.csv` | `DailyTelemetry` |
| `12_guncel_riskler.csv` | `RiskAssessment / RiskAssessments` |
| `13_uyarilar.csv` | `Alert / Alerts` |
| `14_bakim_planlari.csv` | `MaintenancePlan / MaintenancePlans` |
| `15_kod_listeleri.csv` | tanım tabloları |

`11_ml_egitim_verisi.csv` ML ekibinin eğitim/özellik veri setidir; backend bunu ana işlem tablosu olarak içe aktarmaz. Backend, ML sonucunu callback API üzerinden alır.

## Fiziksel şema ve uygulama modeli

Teslim şeması SQL Server 2025 / compatibility level 170 kullanır (`nvarchar`, `datetime2`).
Development ekran okumaları `BakimYonetimiDbContext` ile bu fiziksel adları doğrudan kullanır.
Mevcut `ApplicationDbContext` ise iş akışı/entegrasyon komut modeli olarak korunmuştur:

- İsimler `snake_case` üretilir.
- Tarih-saatler `DateTimeOffset` / `timestamp with time zone` ve UTC kullanır.
- Algoritma ayrıntıları ile entegrasyon payloadları `jsonb` saklanır.
- Para alanları `numeric(18,2)`, riskler `numeric(5,2)` kullanır.
- Risk puanlarında `0..100`, hibrit ağırlıklarda toplam `1` check constraint vardır.
- Risk seviyesi enum değerleri veritabanında `Low`, `Medium`, `High`, `Critical`; API cevabında `Düşük`, `Orta`, `Yüksek`, `Kritik` olarak sunulur.

Merkezi eşleme dosyası: `OnleyiciBakim/Data/ApplicationDbContext.cs`.

## Kaynak şema kuralları

1. Tablo veya sütun eklemeyin/değiştirmeyin; merkezi güncel yedek yeniden dağıtılmalıdır.
2. Fiziksel eşleme `BakimYonetimiDbContext` ve `Data/BakimYonetimi/Entities` altındadır.
3. API sözleşmeleri Türkçe fiziksel şemadan bağımsız tutulur; dönüşüm salt-okunur adaptörde yapılır.
4. Aşağıdaki indeksleri koruyun:
   - `machine(code)` unique
   - `failure_record(machine_id, occurred_at)`
   - `maintenance_record(machine_id, performed_at)`
   - `daily_telemetry(machine_id, date)` unique
   - `risk_assessment(machine_id, evaluated_at)`
   - `risk_assessment(correlation_id)` unique
   - `maintenance_plan(status, planned_at)`
   - `alert(status, level, generated_at)`
5. Foreign keylerde makineye bağlı işlem kayıtlarını cascade silmeyin.
6. SQL Server bağlantısını `ConnectionStrings__BakimYonetimiSqlServer` ile verin.

## CSV içe aktarma sırası

Referans bütünlüğü için paket sırası doğrudur:

```text
firma → şube → departman → üretim hattı → personel
→ makine → arıza/bakım/hata → telemetri
→ risk → uyarı → bakım planı
```

CSV dosyaları UTF-8 BOM, virgül ayraç, nokta ondalık ve ISO tarih-saat biçimindedir. CSV'deki 2015 tarihleri gerçek zamanlı dashboard testinde eski kalacağı için demo ortamında tarihleri güncellemek veya dashboard tarih aralığını genişletmek gerekir.

## Veri sorumluluğu sınırı

- Veritabanı ekibi: merkezi SQL Server şeması/yedeği, roller, CSV yükleme ve performans ayarları.
- Backend: EF mapping, iş kuralları, transaction, API ve entegrasyon idempotency.
- ML ekibi: model eğitimi, 0–100 ML skoru, model sürümü/güveni ve açıklama üretimi.
- Frontend: API'nin döndürdüğü `hybridRisk.level`, `colorHex` ve ekran endpointlerini kullanma.
