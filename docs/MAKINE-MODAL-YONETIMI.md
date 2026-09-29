# Makine Modal Yönetimi

Bu belge, makine kartlarında kullanılan modal CRUD kurallarını ve çalışması için gereken ayarları tanımlar.

## Dosyalar ve sorumluluklar

- `Frontend/Views/Machines/_CreateOrEditModal.cshtml`: ekleme/düzenleme modal kabuğu.
- `Frontend/Views/Machines/_MachineFormFields.cshtml`: gerçek SQL şemasına bağlı form alanları.
- `Frontend/Views/Machines/_DetailsModal.cshtml`: genel bilgi, bileşen, arıza, bakım ve telemetri sekmeleri.
- `Frontend/Views/Machines/_DeleteModal.cshtml`: fiziksel silme yerine pasife alma onayı.
- `Frontend/wwwroot/js/modal-crud.js`: modal yükleme, form gönderme, çift tıklama engeli, hata gösterme, toast ve liste yenileme.
- `Frontend/Controllers/MachinesController.cs`: antiforgery doğrulamalı MVC modal uçları.
- `Frontend/Services/MachineManagementClient.cs`: frontend ile backend yönetim API'si arasındaki bağlantı.
- `OnleyiciBakim/Controllers/Api/MachineManagementController.cs`: yönetim API uçları.
- `OnleyiciBakim/Services/Management/MachineManagementService.cs`: benzersiz kod, organizasyon hiyerarşisi, ERP sahipliği ve soft-delete kurallarının uygulandığı yer.
- `OnleyiciBakim/Contracts/Management/MachineManagementContracts.cs`: istek, cevap ve doğrulama sözleşmeleri.

## Veri sahipliği kuralları

- `VeriKaynagi = ERP` olan makine kartı her durumda salt okunurdur.
- `ErpIntegration:UseErpAsMasterSource = true` olduğunda yerel makine ekleme ve değiştirme de kapatılır.
- Yerel makine silinmez. `Durum = Pasif` yapılır; ilişkili arıza, bakım, bileşen ve risk verileri korunur.
- Pasif yerel kayıt listeden tekrar aktifleştirilebilir.

## Çalıştırma ayarları

Backend Development yapılandırmasında aşağıdakiler gereklidir:

```json
"Database": {
  "UseBakimYonetimiReadModel": true
},
"ErpIntegration": {
  "Enabled": false,
  "UseErpAsMasterSource": false
}
```

Frontend için:

```json
"Backend": {
  "BaseUrl": "http://localhost:5224/",
  "UseMockData": false
}
```

ERP gerçek ana kaynak yapılacaksa iki ERP ayarı da `true` yapılmalı ve ERP senkronizasyon ayarları ayrıca geçerli servis adresi/kimlik bilgileriyle etkinleştirilmelidir. ERP kullanılmayacaksa ikisi de `false` kalmalıdır.

Yapay zekâ servisi makine CRUD modallarının çalışması için gerekli değildir. Tahmin ve hibrit bakım hesabı için `MlService:UseMock = false` olmalı ve Python ML servisi `MlService:BaseUrl` adresinde çalışmalıdır.
