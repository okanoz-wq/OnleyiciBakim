# Kestirimci Bakım ML Servisi

Bu klasör, ASP.NET Core backend tarafından HTTP ile çağrılan bağımsız Python 3.11
servisidir. Veritabanına bağlanmaz ve bakım tarihi kaydetmez. Backend 20 özelliği
ERP/uygulama tablolarından hesaplar; Python yalnızca AI risk çıktısı üretir.

## Yapı

```text
MlService/
├── app.py
├── schemas.py
├── model_utils.py
├── train_model.py
├── prepare_training_dataset.py
├── requirements.txt
├── requirements-dev.txt
├── data/11_ml_egitim_verisi.csv
├── model_artifacts/
│   ├── model.pkl
│   ├── feature_config.json
│   ├── model_metadata.json
│   └── evaluation_report.json
└── tests/
```

Bütün çalışma zamanı yolları `Path(__file__).resolve().parent` üzerinden oluşturulur.
Komutlar başka bir çalışma klasöründen çağrılsa da veri ve model dosyaları bulunur.
Eksik dosyanın mutlak yolu servis loguna yazılır; istemciye teknik exception dönmez.

## Python 3.11 kurulumu

```powershell
cd .\MlService
py -3.11 -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade pip
.\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
.\.venv\Scripts\python.exe -m pytest -q
.\.venv\Scripts\python.exe .\train_model.py
.\.venv\Scripts\python.exe .\app.py
```

Production için Flask geliştirme sunucusu yerine:

```powershell
.\.venv\Scripts\waitress-serve.exe --host=0.0.0.0 --port=5001 app:app
```

Servis varsayılan olarak `127.0.0.1:5001` adresindedir. `ML_PORT`,
`ML_MAX_REQUEST_BYTES`, `ML_LOG_LEVEL` ve isteğe bağlı `ML_API_KEY` environment
değişkenleri desteklenir. API anahtarı ayarlanırsa `/predict` ve `/model-info`
çağrılarında `X-Api-Key` gerekir; `/health` orkestratörler için açık kalır.

## API

- `GET /health`
- `GET /model-info` ve `GET /api/v1/model-info`
- `POST /predict` ve `POST /api/v1/predict`

Tahmin isteği:

```json
{
  "machineId": "CNC-001",
  "features": {
    "yas": 12,
    "son_bakimdan_gecen_gun": 8,
    "bakim_sayisi_30g": 1,
    "bakim_sayisi_90g": 3,
    "ariza_sayisi_30g": 0,
    "ariza_sayisi_90g": 1,
    "toplam_gecmis_ariza": 4,
    "hata_sayisi_7g": 1,
    "hata_sayisi_30g": 3,
    "durus_suresi_30g": 45,
    "ort_mudahale_suresi_90g": 38.5,
    "tekrar_ariza_sayisi_90g": 0,
    "volt_ort_7g": 170.2,
    "devir_ort_7g": 445.6,
    "basinc_ort_7g": 98.7,
    "titresim_ort_7g": 41.1,
    "anomali_puani_7g": 4.1,
    "uretim_yogunlugu_7g": 55.0,
    "model": "model3",
    "kritik_seviye": "Yüksek"
  }
}
```

Cevap `aiRiskScore`, `failureProbability`, `aiRiskLevel`,
`modelConfidence`, `dataCompletenessRate`, `missingFields`, `warnings`, `recommendedActions`,
`ruleBasedExplanation`, `modelExplanation`, `modelVersion` ve `predictionDate`
alanlarını içerir. Python kesin bakım tarihi dönmez. `modelExplanation`, tek tahmine
ait SHAP sonucu değil, Random Forest'ın global özellik önemidir ve yöntem adı cevapta
açıkça belirtilir.

`failureProbability` 30 günlük hedef üzerinde üretilmiş kalibre edilmiş model
çıktısıdır. Veri 2015 tarihli demo/eğitim verisidir; saha kalibrasyonu yapılmadan
kesin arıza ihtimali olarak sunulmamalıdır. Arayüzde ana ifade **AI risk skoru**dur.

## Eksik veri ve doğrulama

Tamlık oranı, dolu model özelliği sayısının 20'ye oranıdır; `machineId` sayılmaz.

- `%80–100`: backend AI `%40`, kural `%60` kullanır.
- `%50–79.99`: backend AI `%20`, kural `%80` kullanır.
- `%50 altı`: Python `INSUFFICIENT_DATA` döndürür; backend yalnızca kural skorunu kullanır.

Sayısal eksikler eğitim medyanıyla, kategorik eksikler `Bilinmiyor` ile pipeline
içinde doldurulur. Negatif değerler, geçersiz kritiklik kategorileri ve sayısal alanlara
gönderilen bool değerleri reddedilir. Eğitim min/max aralığı dışındaki değerler ve
görülmemiş makine modelleri tahmini engellemeden `warnings` üretir.

## Model ve değerlendirme

Model; medyan/kategorik imputer, OneHotEncoder ve RandomForestClassifier içeren bir
pipeline'dır. Zaman bazlı holdout üzerinde sigmoid kalibrasyon Brier skorunu
`0.226801` değerinden `0.226665` değerine, log loss'u `0.646694` değerinden
`0.645386` değerine iyileştirdiği için kalibre edilmiş sürüm yayınlandı.

| Değerlendirme | ROC-AUC | Precision | Recall | F1 |
|---|---:|---:|---:|---:|
| Random split | 0.748677 | 0.720096 | 0.628392 | 0.671126 |
| Zaman split | 0.679016 | 0.650980 | 0.660040 | 0.655479 |
| TimeSeriesSplit (5 fold ort.) | 0.691261 | 0.638039 | 0.657159 | 0.646964 |
| Görülmemiş 20 makine | 0.606847 | 0.573386 | 0.620763 | 0.596134 |

Tüm fold metrikleri ve confusion matrix'ler
`model_artifacts/evaluation_report.json` içindedir. Random split ana production
metriği olarak değerlendirilmemelidir; zaman ve görülmemiş makine sonuçları daha
gerçekçi sınırlamayı gösterir.

## Gerçek ERP ile yeniden eğitim

Mevcut CSV gerçek şirket ERP verisi olarak kabul edilmez. `prepare_training_dataset.py`
yalnızca kullanıcı tarafından doğrulanmış mapping ile çalışır. Önce
`erp_mapping.example.json` kopyalanıp bütün `TODO` alanları ERP ekibi tarafından
doldurulmalıdır:

```powershell
.\.venv\Scripts\python.exe .\prepare_training_dataset.py `
  --mapping .\erp_mapping.json `
  --input-dir C:\erp-export `
  --output .\data\erp_ml_egitim_verisi.csv
```

Script her makine/hesaplama tarihi için yalnızca geçmiş pencereleri kullanır;
`gelecek_30g_ariza` etiketi yalnızca hesaplama tarihinden sonraki kayıtlardan üretilir.
