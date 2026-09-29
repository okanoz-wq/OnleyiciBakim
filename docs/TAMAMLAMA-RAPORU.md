# Önleyici Bakım Yönetim Sistemi — Tamamlama Raporu

## 1. İncelenen proje yapısı

ASP.NET Core 8 Web API (`OnleyiciBakim`), MVC frontend (`Frontend`), xUnit test projesi
(`OnleyiciBakim.Tests`) ve Python 3.11 Flask ML servisi (`MlService`) incelendi. DOCX ekran
tasarımları, teslim SQL şeması ve ML ZIP içeriği mevcut kodla karşılaştırıldı.

## 2. Kullanılan veritabanı yaklaşımı

Mevcut Database First yaklaşımı korundu. Normal kullanıcı akışları LocalDB üzerindeki
`BakimYonetimiDb` SQL Server veritabanını kullanır. EF migration zorlanmadı; idempotent manuel
SQL tamamlama scripti ve güncellenmiş entity eşlemeleri kullanıldı.

## 3. Oluşturulan tablolar

`MakineSayacTanimlari`, `Vardiyalar`, `MakineSayacKayitlari`, `MakineCalismaTakvimleri`,
`MakineTransferleri`, `Ekipler`, `EkipUyeleri`, `ArizaDegisenParcalar`,
`IsEmriDurumGecmisi`, `UyariIslemGecmisi`, `BakimPlanDurumGecmisi`.

## 4. Değiştirilen tablolar

`Firmalar`, `Subeler`, `Departmanlar`, `UretimHatlari`, `Makineler`,
`BakimKontrolListeleri`, `HibritKararParametreleri`, `BakimOneriParametreleri`,
`ArizaKartlari`, `BakimPlanlari`, `BakimKayitlari`, `RiskAnalizleri`, `GuncelRiskler`,
`Uyarilar` güvenli `ALTER` ve backfill işlemleriyle tamamlandı.

## 5. Eklenen kolonlar

Audit, açıklama, aktif/pasif ve rowversion alanları; makine tür/marka/seri/kurulum/periyot
alanları; arıza bildirim/kapanış alanları; planın analiz/uyarı/tür/ekip/onay alanları;
bakımın plan/tür/zaman/kaynak/checklist alanları; riskin AI skoru, tamlık, güven, snapshot,
uyarı ve hesaplama nedeni; güncel risk model ayrıntıları; uyarı işlem sonucu alanları eklendi.

## 6. Eklenen foreign key'ler

Yeni operasyon tabloları makine, vardiya, sayaç, personel, ekip, arıza müdahalesi, iş emri,
uyarı ve bakım planlarına bağlandı. Arıza kodu, plan–analiz/uyarı/tür/ekip ve bakım–plan/tür/
checklist ilişkileri eklendi. Silmeler `RESTRICT/NO ACTION` yaklaşımıyla kontrollüdür.

## 7. Eklenen unique index ve check constraint'ler

Organizasyon kodları, makine kodu, sayaç/vardiya/ekip kodları, makine başına güncel risk,
makine–sayaç–tarih–vardiya kaydı ve tek aktif hibrit plan için unique indexler eklendi.
Sayaç sırası, negatif süre/değer, risk 0–100, hibrit ağırlık toplamı ve öneri aralığı check
constraint'leri eklendi.

## 8. Oluşturulan migration veya SQL scriptleri

[`scripts/20260805_PreventiveMaintenanceCompletion.sql`](../scripts/20260805_PreventiveMaintenanceCompletion.sql)
idempotenttir, iki kez başarıyla uygulanmıştır. Uygulama öncesi yedek alınmış ve
`RESTORE VERIFYONLY` ile doğrulanmıştır.

## 9. Oluşturulan entity sınıfları

Yeni 11 tablo [`OperationalEntities.cs`](../OnleyiciBakim/Data/BakimYonetimi/Entities/OperationalEntities.cs)
dosyasında; DbSet ve ilişki eşlemeleri `BakimYonetimiDbContext.Extensions.cs` dosyasındadır.
Mevcut 14 entity yeni kolonlarla genişletilmiştir.

## 10. Oluşturulan servisler

`LocalRiskAssessmentService`, `LocalDefinitionService`, `LocalWorkflowService` ve
`BakimYonetimiReportService`; frontend'de makine, tanım, operasyon ve rapor HTTP istemcileri.

## 11. Oluşturulan controller'lar

Backend'de `MachineManagementController`, `LocalDefinitionsController`,
`LocalWorkflowsController` ve yerel risk endpoint'i; frontend'de `DefinitionsController`,
`OperationsController` ve genişletilmiş `MachinesController`.

## 12. Oluşturulan ViewModel'ler

Makine yönetimi, genel tanım/parametre modalı ve operasyon iş akışı için ayrı create/edit
ViewModel'leri kullanıldı. Hesaplanan risk alanları hiçbir upsert modelinde bulunmaz.

## 13. Oluşturulan modal partial view'lar

Makine için create/edit/details/delete ve bağlı özetler; tanımlar için genel create/edit,
details/delete; operasyonlar için iş akışı modalı; plan ve arıza detay modalları eklendi.

## 14. Eklenen JavaScript ve CSS dosyaları

`modal-crud.js` modal yükleme, validation, çift-submit kilidi, toast, bölgesel tablo yenileme,
organizasyon cascade'i ve dinamik checklist satırlarını yönetir. `site.css` mevcut tasarım dili
korunarak gruplu menü ve modal bileşenleriyle genişletildi.

## 15. Risk hesaplama yapısı

SQL verileri `BakimYonetimiMachineFeatureService` ile 20 özelliğe dönüştürülür; kural skoru ve
ML sonucu ayrı hesaplanır, ortak `RiskClassificationService` sınırları uygular. Her hesaplama
geçmiş analize yazılır, makine başına tek `GuncelRiskler` satırı upsert edilir.

## 16. Hibrit skor formülü

Tamlık ≥80 için `AI×0.40 + Kural×0.60`; 50–79.99 için `AI×0.20 + Kural×0.80`;
50 altı veya ML hatasında `Kural×1.00`. Sonuç 0–100'e sınırlandırılır. Aralıklar
`[0,30) Düşük`, `[30,60) Orta`, `[60,80) Yüksek`, `[80,100] Kritik`tir.

## 17. Bakım planlama kuralları

Nihai hibrit seviye üzerinden sırasıyla 60/21/7/3 gün kullanılır. Aynı makinenin aktif hibrit
planı varsa güncellenir; filtered unique index de mükerrerliği veritabanında engeller.

## 18. AI entegrasyon akışı

SQL Server → ASP.NET özellik servisi → Python `/api/v1/predict` → kural + hibrit karar → SQL.
Python veritabanına bağlanmaz ve bakım tarihi üretmez. `MlOlasiligi` 0–1, `AiRiskSkoru` 0–100
standardında saklanır; snapshot ve model sürümü kaydedilir.

## 19. Otomatik analiz çalışma şekli

`RiskRecalculationWorker` periyodik olarak aktif yerel makineleri işler. Sayaç, arıza ve bakım
değişiklikleri API üzerinden ilgili makine için ayrıca analiz tetikler. ML hatası loglanır ve
kural tabanlı sonuçla işlem devam eder.

## 20. Çalıştırılan testler

Risk sınırları/ağırlıkları/fallback, API, makine ve tanım CRUD kuralları, organizasyon,
checklist, sayaç, arıza müdahalesi/parçaları, kapalı arıza, bakım–plan bağlantısı, tek aktif
plan, anti-forgery, SQL duman testi ve Python API/model testleri çalıştırıldı.

## 21. Test sonuçları

`.NET`: 66 başarılı, 0 başarısız, 0 atlanan. Python: 25 başarılı. Gerçek HTTP duman testinde
tanım ve rapor ekranları 200; backend canlılık `Healthy`; 15 tanım modülü erişilebilir.

## 22. dotnet build sonucu

Solution build sonucu: 0 hata, 0 uyarı.

## 23. Python servis test sonucu

Python 3.11.9. `/health`: `ok`, model `rf-v2-time-aware`. Gerçek `/predict`: AI skoru 47.3,
olasılık 0.4727, tamlık %100. Backend uçtan uca doğrulamasında `MAK-0002` için nihai skor
60.00/Yüksek ve `LOCAL` SQL kaydı oluştu.

## 24. Veri kaybı riskleri ve alınan önlemler

Database/table drop yapılmadı; mevcut kayıt silinmedi. Script önce nullable/backfill yaklaşımı,
idempotent kontroller ve kontrollü FK kullanır. Fiziksel silme yerine pasife alma uygulanır.
Yedek: `C:\Users\Kaan\Downloads\BakimYonetimiDb_before_20260805.bak`.

## 25. TODO_DECISION_REQUIRED maddeleri

Yok. `UretimHatlari` mevcut veri ve ilişkilerde dokümandaki İş Merkezi kavramını karşıladığı
için yeni ve mükerrer `IsMerkezleri` tablosu oluşturulmadı; UI etiketi “İş Merkezi” yapıldı.

## 26. Bilinen sınırlamalar

Authentication mevcut projede bulunmadığından yarım bir kimlik sistemi eklenmedi. Yetki bazlı
alan değişimi ileride gerçek authentication ile bağlanmalıdır. Tarihsel demo ML modeli saha
verisiyle kalibre edilmeden kesin arıza olasılığı kabul edilmemelidir. Eski PostgreSQL uyumluluk
katmanı kodda durur fakat normal MVC yerel SQL akışında kullanılmaz.

## 27. Manuel kontrol edilmesi gereken noktalar

Production SQL bağlantı secret'ı ortam değişkeninden verilmelidir. Gerçek personel yetkileri,
bildirim kanalı, saha sayaç birimleri ve ERP tekrar açılacaksa mapping kullanıcı/iş birimi
tarafından onaylanmalıdır. Yayın öncesi yedek, script dry-run, mobil tarayıcı modal kontrolü ve
ML servisinin Windows service/container olarak çalıştırılması doğrulanmalıdır.
