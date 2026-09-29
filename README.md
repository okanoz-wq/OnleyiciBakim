# Önleyici Bakım Sistemi

ASP.NET Core 8 tabanlı çözüm; merkezi `BakimYonetimiDb` SQL Server şemasını normal kullanıcı
akışlarında hem okuma hem yazma modeli olarak kullanır. ERP kapalıdır ve menüde gösterilmez;
mevcut ERP kolonları gelecekte kullanım için korunmuştur. Python ML servisi veritabanına
bağlanmaz, yalnızca backend'in ürettiği 20 özellik üzerinden tahmin verir.

## Projeler

- `OnleyiciBakim`: REST API, SQL Server ekran modeli, PostgreSQL komut modeli, ML/ERP adapter'ları ve Development
  test paneli.
- `Frontend`: backend API'sini kullanan MVC frontend.
- `OnleyiciBakim.Tests`: unit ve API integration testleri.
- `MlService`: Python 3.11, Random Forest tabanlı bağımsız AI risk servisi ve eğitim/test araçları.

Detaylı backend teslim dokümanı: [BACKEND_README.md](BACKEND_README.md)  
Frontend sözleşmesi: [FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md)  
HTTP örnekleri: [OnleyiciBakim.http](OnleyiciBakim.http)

## Hızlı başlangıç

Her şema güncellemesinden önce SQL Server yedeği alın. Bu teslimde doğrulanmış geri dönüş
noktası `C:\Users\Kaan\Downloads\BakimYonetimiDb_before_20260805.bak` dosyasıdır.
İlk kurulumda gönderilen en güncel yedeği LocalDB'ye geri yükleyin:

```powershell
.\scripts\Restore-BakimYonetimiDb.ps1
```

Betik mevcut bir `BakimYonetimiDb` üzerine yazmaz. Bilinçli yenileme gerekiyorsa `-Replace`
kullanılabilir. Yedeğin ardından idempotent tamamlama scriptini uygulayın; script veritabanı
veya iş tablosu silmez ve tekrar çalıştırılabilir:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d BakimYonetimiDb -E -b -f 65001 `
  -i .\scripts\20260805_PreventiveMaintenanceCompletion.sql
```

SQL betiği daha önce `-f 65001` olmadan çalıştırıldıysa Türkçe metinleri ve bu metinleri
kullanan veritabanı nesnelerini bir kez onarın:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d BakimYonetimiDb -E -b -f 65001 `
  -i .\scripts\20260806_RepairTurkishText.sql
```

Ardından Development profili yerel SQL Server ve gerçek yerel ML HTTP servisini kullanır:

```powershell
dotnet restore .\OnleyiciBakim.sln
dotnet build .\OnleyiciBakim.sln
dotnet test .\OnleyiciBakim.sln
dotnet run --project .\OnleyiciBakim\OnleyiciBakim.csproj
```

ML servisini ayrı terminalde başlatın:

```powershell
cd .\MlService
py -3.11 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
.\.venv\Scripts\python.exe .\app.py
```

ML ayrıntıları ve güncel model metrikleri: [MlService/README.md](MlService/README.md)

Backend:

- API: `http://localhost:5224/api/v1`
- Swagger: `http://localhost:5224/swagger`
- Development test paneli: `http://localhost:5224/dev-test`
- Health: `http://localhost:5224/health`, `/health/live`, `/health/ready`

Frontend'i ayrı terminalde başlatın:

```powershell
dotnet run --project .\Frontend\OnleyiciBakimSistemi.csproj
```

Frontend: `http://localhost:5080`

## Veritabanı çalışma biçimi

- `BakimYonetimiDbContext`, 44 iş tablosunu ve ilişkilerini Database First + güvenli manuel
  SQL yaklaşımıyla eşler.
- Dashboard, tanımlar, makineler, sayaçlar, arıza/bakım/uyarı/iş emri akışları ve raporlar
  gerçek SQL Server verisini okur ve yazar.
- Kaynak şema uygulama başlangıcında değiştirilmez; migration veya `EnsureCreated` çalıştırılmaz.
- Yeni yerel iş kayıtlarında `VeriKaynagi = "LOCAL"` kullanılır. Eski PostgreSQL komut modeli
  uyumluluk amacıyla kodda tutulur ancak MVC'nin normal bakım yönetimi akışında kullanılmaz.
- Uygulama başlangıçta destructive veritabanı işlemi çalıştırmaz.

## Kesin risk kuralı

- `[0,30)`: Düşük / `green`
- `[30,60)`: Orta / `amber`
- `[60,80)`: Yüksek / `red`
- `[80,100]`: Kritik / `dark-red`

Hibrit skor veri tamlığına göre ara yuvarlama yapılmadan hesaplanır:

- Tamlık `%80+`: `AI × 0.40 + kural × 0.60`
- Tamlık `%50–79`: `AI × 0.20 + kural × 0.80`
- Tamlık `%50 altı` veya ML kesintisi: yalnızca kural skoru

Kritik telemetri ve son 7 gündeki tekrarlayan kritik arıza skoru en az 80'e;
geçmiş zorunlu bakım tarihi en az 60'a yükseltir. Kesin bakım tarihi AI cevabından
değil, nihai hibrit seviyeden (`60/21/7/3` gün) backend tarafından oluşturulur.
