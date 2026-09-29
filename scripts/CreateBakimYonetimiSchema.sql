-- =====================================================================
-- BakimYonetimiDb - bos sema olusturma script'i (EF Core modelinden uretildi)
-- Bu script, orijinal .bak yedeginin YERINE GECMEZ; sadece uygulamanin
-- calismasi icin gereken 44 tabloyu, index'leri ve foreign key'leri bos
-- olarak olusturur. Veri icermez.
-- =====================================================================

IF DB_ID(N'BakimYonetimiDb') IS NULL
BEGIN
    CREATE DATABASE [BakimYonetimiDb];
END
GO
USE [BakimYonetimiDb];
GO

IF OBJECT_ID(N'dbo.ArizaKartlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ArizaKartlari] (
    [ArizaId] nvarchar(20) NOT NULL,
    [ArizaTarihi] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [ArizaKoduId] int NULL,
    [VardiyaId] int NULL,
    [BilesenKodu] nvarchar(20) NULL,
    [BilesenAdi] nvarchar(100) NULL,
    [ArizaTuru] nvarchar(150) NULL,
    [OnemSeviyesi] nvarchar(20) NULL,
    [TekrarEdenMi] bit NOT NULL,
    [PlanliDurusMu] bit NOT NULL,
    [IlkMudahaleDk] int NULL,
    [MudahaleSuresiDk] int NULL,
    [DurusSuresiDk] int NULL,
    [Durum] nvarchar(30) NULL,
    [KokNeden] nvarchar(200) NULL,
    [Aciklama] nvarchar(500) NULL,
    [PersonelId] nvarchar(20) NULL,
    [IsEmriNo] nvarchar(50) NULL,
    [TahminiMaliyetTl] decimal(18,2) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [UrunStokKodu] nvarchar(100) NULL,
    [IlkBelirti] nvarchar(1000) NULL,
    [UretimDurduMu] bit NOT NULL,
    [TahminiDurusSuresiDk] int NULL,
    [BildirenPersonelId] nvarchar(20) NULL,
    [ArizaBitisTarihi] datetime2 NULL,
    [UretimKaybiMiktari] decimal(18,3) NULL,
    [UretimKaybiBirimi] nvarchar(30) NULL,
    [Aktif] bit NOT NULL,
    [KapanisPersonelId] nvarchar(20) NULL,
    [KapanisTarihi] datetime2 NULL,
    [CozumAciklamasi] nvarchar(2000) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_ArizaKartlari] PRIMARY KEY CLUSTERED ([ArizaId])
);
END
GO

IF OBJECT_ID(N'dbo.ArizaKoduTanimlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ArizaKoduTanimlari] (
    [ArizaKoduId] int IDENTITY(1,1) NOT NULL,
    [ArizaKodu] nvarchar(20) NOT NULL,
    [ArizaAdi] nvarchar(150) NOT NULL,
    [ArizaKategorisi] nvarchar(50) NULL,
    [VarsayilanOnemSeviyesi] nvarchar(20) NULL,
    [TekrarKontrolSuresiGun] int NULL,
    [MakineTuru] nvarchar(50) NULL,
    [IlgiliBilesenTuru] nvarchar(50) NULL,
    [UretimiDurdururMu] bit NOT NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_ArizaKoduTanimlari] PRIMARY KEY CLUSTERED ([ArizaKoduId])
);
END
GO

IF OBJECT_ID(N'dbo.ArizaMudahaleler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ArizaMudahaleler] (
    [MudahaleId] int IDENTITY(1,1) NOT NULL,
    [ArizaId] nvarchar(20) NOT NULL,
    [MudahaleBaslangic] datetime2 NOT NULL,
    [MudahaleBitis] datetime2 NULL,
    [PersonelId] nvarchar(20) NULL,
    [YapilanIslem] nvarchar(1000) NULL,
    [DegisenParca] nvarchar(200) NULL,
    [KokNeden] nvarchar(300) NULL,
    [Sonuc] nvarchar(100) NULL,
    [Durum] nvarchar(50) NULL,
    [GercekDurusSuresiDk] int NULL,
    [Aciklama] nvarchar(1000) NULL,
    CONSTRAINT [PK_ArizaMudahaleler] PRIMARY KEY CLUSTERED ([MudahaleId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimDegisenParcalar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimDegisenParcalar] (
    [KayitId] int IDENTITY(1,1) NOT NULL,
    [BakimId] nvarchar(20) NOT NULL,
    [ParcaKodu] nvarchar(20) NOT NULL,
    [Miktar] decimal(8,2) NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(300) NULL,
    CONSTRAINT [PK_BakimDegisenParcalar] PRIMARY KEY CLUSTERED ([KayitId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimKayitlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimKayitlari] (
    [BakimId] nvarchar(20) NOT NULL,
    [BakimTarihi] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [PlanId] nvarchar(20) NULL,
    [BakimTuruId] int NULL,
    [BilesenKodu] nvarchar(20) NULL,
    [BilesenAdi] nvarchar(100) NULL,
    [BakimTuru] nvarchar(30) NULL,
    [BakimNedeni] nvarchar(200) NULL,
    [SureDk] int NOT NULL,
    [Durum] nvarchar(30) NULL,
    [Sonuc] nvarchar(200) NULL,
    [PersonelId] nvarchar(20) NULL,
    [SonrakiBakimTarihi] datetime2 NULL,
    [MaliyetTl] decimal(18,2) NULL,
    [BagliArizaId] nvarchar(20) NULL,
    [IsEmriNo] nvarchar(50) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [BaslangicTarihi] datetime2 NULL,
    [BitisTarihi] datetime2 NULL,
    [KaynakTuru] nvarchar(30) NULL,
    [MakineSonrasiDurum] nvarchar(50) NULL,
    [KontrolListesiId] int NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_BakimKayitlari] PRIMARY KEY CLUSTERED ([BakimId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimKontrolListeleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimKontrolListeleri] (
    [KontrolListesiId] int IDENTITY(1,1) NOT NULL,
    [KontrolListesiKodu] nvarchar(20) NOT NULL,
    [KontrolListesiAdi] nvarchar(150) NOT NULL,
    [BilesenKodu] nvarchar(20) NULL,
    [BakimTuruKodu] nvarchar(20) NULL,
    [MakineTuru] nvarchar(100) NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    CONSTRAINT [PK_BakimKontrolListeleri] PRIMARY KEY CLUSTERED ([KontrolListesiId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimKontrolSonuclari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimKontrolSonuclari] (
    [SonucId] int IDENTITY(1,1) NOT NULL,
    [BakimId] nvarchar(20) NOT NULL,
    [MaddeId] int NULL,
    [SiraNo] int NOT NULL,
    [KontrolMaddesi] nvarchar(300) NOT NULL,
    [Sonuc] nvarchar(50) NULL,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_BakimKontrolSonuclari] PRIMARY KEY CLUSTERED ([SonucId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimOneriParametreleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimOneriParametreleri] (
    [OneriId] int IDENTITY(1,1) NOT NULL,
    [OneriKodu] nvarchar(20) NOT NULL,
    [RiskSeviyesi] nvarchar(20) NOT NULL,
    [MinPuan] decimal(5,2) NOT NULL,
    [MaxPuan] decimal(5,2) NOT NULL,
    [MaxDahil] bit NOT NULL,
    [OnerilenGun] int NOT NULL,
    [VarsayilanBakimTuru] nvarchar(50) NULL,
    [VarsayilanOncelik] nvarchar(20) NULL,
    [OtomatikUyariOlustur] bit NOT NULL DEFAULT 1,
    [OtomatikPlanTaslagi] bit NOT NULL,
    [YoneticiOnayiGerekli] bit NOT NULL,
    [GerekceZorunlu] bit NOT NULL,
    [BildirimGoster] bit NOT NULL DEFAULT 1,
    [IslemGerekmiyorSecenegi] bit NOT NULL,
    [VarsayilanKarar] nvarchar(50) NULL,
    [SorumluRol] nvarchar(50) NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_BakimOneriParametreleri] PRIMARY KEY CLUSTERED ([OneriId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimPlanGorevleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimPlanGorevleri] (
    [GorevId] int IDENTITY(1,1) NOT NULL,
    [PlanId] nvarchar(20) NOT NULL,
    [SiraNo] int NOT NULL,
    [Gorev] nvarchar(300) NOT NULL,
    [SorumluPersonelId] nvarchar(20) NULL,
    [TahminiSureSaat] decimal(5,2) NULL,
    [Durum] nvarchar(50) NULL DEFAULT 'Planlandi',
    CONSTRAINT [PK_BakimPlanGorevleri] PRIMARY KEY CLUSTERED ([GorevId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimPlanlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimPlanlari] (
    [PlanId] nvarchar(20) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [AnalizId] int NULL,
    [UyariId] nvarchar(20) NULL,
    [OnerilenTarih] date NULL,
    [BakimTuruId] int NULL,
    [SorumluEkipId] int NULL,
    [BilesenKodu] nvarchar(20) NULL,
    [BilesenAdi] nvarchar(100) NULL,
    [PlanlananTarih] datetime2 NOT NULL,
    [BakimTuru] nvarchar(30) NULL,
    [Oncelik] nvarchar(20) NULL,
    [TahminiSureDk] int NOT NULL,
    [TahminiMaliyetTl] decimal(18,2) NULL,
    [SorumluPersonelId] nvarchar(20) NULL,
    [Durum] nvarchar(30) NULL,
    [Gerekce] nvarchar(1000) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [OnayDurumu] nvarchar(30) NULL,
    [OnaylayanPersonelId] nvarchar(20) NULL,
    [OnayTarihi] datetime2 NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_BakimPlanlari] PRIMARY KEY CLUSTERED ([PlanId])
);
END
GO

IF OBJECT_ID(N'dbo.BakimTuruTanimlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimTuruTanimlari] (
    [BakimTuruId] int IDENTITY(1,1) NOT NULL,
    [BakimTuruKodu] nvarchar(20) NOT NULL,
    [BakimTuruAdi] nvarchar(100) NOT NULL,
    [Tur] nvarchar(20) NULL,
    [PlanTuru] nvarchar(20) NULL,
    [VarsayilanSureSaat] decimal(6,2) NULL,
    [VarsayilanOncelik] nvarchar(20) NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_BakimTuruTanimlari] PRIMARY KEY CLUSTERED ([BakimTuruId])
);
END
GO

IF OBJECT_ID(N'dbo.Departmanlar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Departmanlar] (
    [DepartmanId] nvarchar(20) NOT NULL,
    [SubeId] nvarchar(20) NOT NULL,
    [DepartmanAdi] nvarchar(200) NULL,
    [DepartmanKodu] nvarchar(50) NULL,
    [AktifMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Departmanlar] PRIMARY KEY CLUSTERED ([DepartmanId])
);
END
GO

IF OBJECT_ID(N'dbo.Firmalar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Firmalar] (
    [FirmaId] nvarchar(20) NOT NULL,
    [FirmaAdi] nvarchar(200) NULL,
    [FirmaKodu] nvarchar(50) NULL,
    [AktifMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Firmalar] PRIMARY KEY CLUSTERED ([FirmaId])
);
END
GO

IF OBJECT_ID(N'dbo.GuncelRiskler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[GuncelRiskler] (
    [RiskId] nvarchar(20) NOT NULL,
    [DegerlendirmeTarihi] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [RiskPuani] decimal(5,2) NOT NULL,
    [RiskSeviyesi] nvarchar(20) NULL,
    [SonBakimdanGecenGun] int NOT NULL,
    [ArizaSayisi30g] int NOT NULL,
    [HataSayisi7g] int NOT NULL,
    [AnomaliPuani7g] decimal(10,4) NOT NULL,
    [RiskNedenleri] nvarchar(1000) NULL,
    [OnerilenAksiyon] nvarchar(500) NULL,
    [Durum] nvarchar(30) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [KuralPuani] decimal(5,2) NULL,
    [IstatistikPuani] decimal(5,2) NULL,
    [MlOlasiligi] decimal(6,4) NULL,
    [ModelDevrede] bit NULL,
    [ModelVersiyonu] nvarchar(50) NULL,
    [VeriYeterliligi] nvarchar(20) NULL,
    [GuvenSeviyesi] nvarchar(20) NULL,
    CONSTRAINT [PK_GuncelRiskler] PRIMARY KEY CLUSTERED ([RiskId])
);
END
GO

IF OBJECT_ID(N'dbo.HataKayitlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[HataKayitlari] (
    [HataId] nvarchar(20) NOT NULL,
    [HataTarihi] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [HataKodu] nvarchar(20) NULL,
    [HataMesaji] nvarchar(200) NULL,
    [Seviye] nvarchar(20) NULL,
    [Durum] nvarchar(30) NULL,
    [CozulmeTarihi] datetime2 NOT NULL,
    [BagliArizaId] nvarchar(20) NULL,
    [OtomatikUyariMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    CONSTRAINT [PK_HataKayitlari] PRIMARY KEY CLUSTERED ([HataId])
);
END
GO

IF OBJECT_ID(N'dbo.HibritKararParametreleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[HibritKararParametreleri] (
    [HibritSetId] int IDENTITY(1,1) NOT NULL,
    [SetKodu] nvarchar(20) NOT NULL,
    [SetAdi] nvarchar(150) NOT NULL,
    [ModelAdi] nvarchar(100) NULL,
    [ModelVersiyonu] nvarchar(20) NULL,
    [KuralSetiId] int NULL,
    [MlAgirligi] int NOT NULL DEFAULT 70,
    [KuralAgirligi] int NOT NULL DEFAULT 30,
    [MinGecmisKayit] int NULL,
    [ModelGuvenEsigi] int NULL,
    [EksikVeriYontemi] nvarchar(50) NULL,
    [VeriYeterlilikOrani] int NULL,
    [UyumsuzlukEsigi] int NULL,
    [BuyukFarktaEngelle] bit NOT NULL,
    [ManuelIncelemeGerekli] bit NOT NULL,
    [OtomatikPlanTaslagi] bit NOT NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_HibritKararParametreleri] PRIMARY KEY CLUSTERED ([HibritSetId])
);
END
GO

IF OBJECT_ID(N'dbo.IsEmirleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[IsEmirleri] (
    [IsEmriId] int IDENTITY(1,1) NOT NULL,
    [IsEmriNo] nvarchar(25) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [PlanId] nvarchar(20) NULL,
    [ArizaId] nvarchar(20) NULL,
    [DevirKaynagi] nvarchar(50) NULL,
    [Oncelik] nvarchar(20) NULL,
    [PlanlananTarih] date NULL,
    [SorumluPersonelId] nvarchar(20) NULL,
    [Durum] nvarchar(50) NULL,
    [ErpSistemi] nvarchar(50) NULL,
    [ErpDurumu] nvarchar(50) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL DEFAULT sysdatetime(),
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_IsEmirleri] PRIMARY KEY CLUSTERED ([IsEmriId])
);
END
GO

IF OBJECT_ID(N'dbo.KararGecmisi', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[KararGecmisi] (
    [KararId] int IDENTITY(1,1) NOT NULL,
    [KararNo] nvarchar(25) NOT NULL,
    [AnalizId] int NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [KararTarihi] datetime2 NOT NULL DEFAULT sysdatetime(),
    [SistemOnerisi] nvarchar(100) NULL,
    [KullaniciKarari] nvarchar(100) NULL,
    [KararVerenPersonelId] nvarchar(20) NULL,
    [Uyumlu] bit NULL,
    [DegisiklikGerekcesi] nvarchar(500) NULL,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_KararGecmisi] PRIMARY KEY CLUSTERED ([KararId])
);
END
GO

IF OBJECT_ID(N'dbo.KodListeleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[KodListeleri] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [KodGrubu] nvarchar(50) NOT NULL,
    [Kod] nvarchar(50) NOT NULL,
    [Aciklama] nvarchar(300) NULL,
    [SiraNo] int NOT NULL,
    CONSTRAINT [PK_KodListeleri] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.KontrolListesiMaddeleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[KontrolListesiMaddeleri] (
    [MaddeId] int IDENTITY(1,1) NOT NULL,
    [KontrolListesiId] int NOT NULL,
    [SiraNo] int NOT NULL,
    [KontrolMaddesi] nvarchar(300) NOT NULL,
    [Zorunlu] bit NOT NULL DEFAULT 1,
    [AciklamaGerekli] bit NOT NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    CONSTRAINT [PK_KontrolListesiMaddeleri] PRIMARY KEY CLUSTERED ([MaddeId])
);
END
GO

IF OBJECT_ID(N'dbo.MakineBilesenler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MakineBilesenler] (
    [BilesenId] int IDENTITY(1,1) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [BilesenKodu] nvarchar(20) NOT NULL,
    [BilesenAdi] nvarchar(100) NOT NULL,
    [BilesenTuru] nvarchar(50) NULL,
    [Kritiklik] nvarchar(20) NULL,
    [TahminiOmur] int NULL,
    [OmurBirimi] nvarchar(20) NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_MakineBilesenler] PRIMARY KEY CLUSTERED ([BilesenId])
);
END
GO

IF OBJECT_ID(N'dbo.Makineler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Makineler] (
    [MakineId] nvarchar(20) NOT NULL,
    [KaynakMakineId] int NULL,
    [FirmaId] nvarchar(20) NOT NULL,
    [SubeId] nvarchar(20) NOT NULL,
    [DepartmanId] nvarchar(20) NOT NULL,
    [HatId] nvarchar(20) NOT NULL,
    [MakineKodu] nvarchar(50) NOT NULL,
    [MakineAdi] nvarchar(200) NOT NULL,
    [MakineTuru] nvarchar(100) NULL,
    [Marka] nvarchar(100) NULL,
    [Model] nvarchar(50) NULL,
    [SeriNo] nvarchar(100) NULL,
    [Yas] int NOT NULL,
    [KritikSeviye] nvarchar(20) NULL,
    [KurulumYili] int NOT NULL,
    [KurulumTarihi] date NULL,
    [Durum] nvarchar(30) NULL,
    [SonBakimTarihi] datetime2 NULL,
    [SonArizaTarihi] datetime2 NULL,
    [GuncelRiskPuani] decimal(5,2) NULL,
    [GuncelRiskSeviyesi] nvarchar(20) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [PeriyotTuru] nvarchar(50) NULL,
    [PeriyotDegeri] decimal(12,2) NULL,
    [PeriyotBirimi] nvarchar(30) NULL,
    [Aktif] bit NOT NULL,
    [Aciklama] nvarchar(1000) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Makineler] PRIMARY KEY CLUSTERED ([MakineId])
);
END
GO

IF OBJECT_ID(N'dbo.Parcalar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Parcalar] (
    [ParcaId] int IDENTITY(1,1) NOT NULL,
    [ParcaKodu] nvarchar(20) NOT NULL,
    [ParcaAdi] nvarchar(150) NOT NULL,
    [BilesenTuru] nvarchar(50) NULL,
    [Birim] nvarchar(20) NULL DEFAULT 'Adet',
    [BirimFiyatTl] decimal(10,2) NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Parcalar] PRIMARY KEY CLUSTERED ([ParcaId])
);
END
GO

IF OBJECT_ID(N'dbo.Personeller', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Personeller] (
    [PersonelId] nvarchar(20) NOT NULL,
    [AdSoyad] nvarchar(200) NULL,
    [Rol] nvarchar(100) NULL,
    [Uzmanlik] nvarchar(100) NULL,
    [AktifMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    CONSTRAINT [PK_Personeller] PRIMARY KEY CLUSTERED ([PersonelId])
);
END
GO

IF OBJECT_ID(N'dbo.RiskAnalizFaktorleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RiskAnalizFaktorleri] (
    [FaktorId] int IDENTITY(1,1) NOT NULL,
    [AnalizId] int NOT NULL,
    [SiraNo] int NOT NULL,
    [Faktor] nvarchar(150) NOT NULL,
    [Deger] nvarchar(100) NULL,
    [Etki] nvarchar(20) NULL,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_RiskAnalizFaktorleri] PRIMARY KEY CLUSTERED ([FaktorId])
);
END
GO

IF OBJECT_ID(N'dbo.RiskAnalizleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RiskAnalizleri] (
    [AnalizId] int IDENTITY(1,1) NOT NULL,
    [AnalizNo] nvarchar(25) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [AnalizTarihi] datetime2 NOT NULL DEFAULT sysdatetime(),
    [KuralSetiId] int NULL,
    [HibritSetId] int NULL,
    [KuralPuani] decimal(5,2) NULL,
    [MlOlasiligi] decimal(6,4) NULL,
    [AiRiskSkoru] decimal(5,2) NULL,
    [ModelVersiyonu] nvarchar(20) NULL,
    [MlAgirligi] int NULL,
    [KuralAgirligi] int NULL,
    [NihaiPuan] decimal(5,2) NOT NULL,
    [RiskSeviyesi] nvarchar(20) NULL,
    [VeriYeterliligi] nvarchar(20) NULL,
    [VeriTamlikOrani] decimal(5,2) NULL,
    [GuvenSeviyesi] decimal(5,2) NULL,
    [GirdiSnapshotJson] nvarchar(max) NULL,
    [UyarilarJson] nvarchar(max) NULL,
    [HesaplamaNedeni] nvarchar(200) NULL,
    [VeriKaynagi] nvarchar(50) NOT NULL,
    [OnerilenBakimTarihi] date NULL,
    [Durum] nvarchar(50) NULL DEFAULT 'Tamamlandi',
    CONSTRAINT [PK_RiskAnalizleri] PRIMARY KEY CLUSTERED ([AnalizId])
);
END
GO

IF OBJECT_ID(N'dbo.RiskKuralDetaylari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RiskKuralDetaylari] (
    [KuralId] int IDENTITY(1,1) NOT NULL,
    [KuralSetiId] int NOT NULL,
    [KuralKodu] nvarchar(20) NOT NULL,
    [RiskFaktoru] nvarchar(100) NOT NULL,
    [ZamanPenceresi] nvarchar(50) NULL,
    [Operator] nvarchar(10) NULL,
    [AltDeger] decimal(10,2) NULL,
    [UstDeger] decimal(10,2) NULL,
    [RiskPuani] int NOT NULL,
    [HardOverride] bit NOT NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    CONSTRAINT [PK_RiskKuralDetaylari] PRIMARY KEY CLUSTERED ([KuralId])
);
END
GO

IF OBJECT_ID(N'dbo.RiskKuralSetleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RiskKuralSetleri] (
    [KuralSetiId] int IDENTITY(1,1) NOT NULL,
    [KuralSetiKodu] nvarchar(20) NOT NULL,
    [KuralSetiAdi] nvarchar(150) NOT NULL,
    [Versiyon] nvarchar(10) NOT NULL DEFAULT '1.0',
    [GecerlilikBaslangic] date NULL,
    [GecerlilikBitis] date NULL,
    [Aktif] bit NOT NULL DEFAULT 1,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_RiskKuralSetleri] PRIMARY KEY CLUSTERED ([KuralSetiId])
);
END
GO

IF OBJECT_ID(N'dbo.Subeler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Subeler] (
    [SubeId] nvarchar(20) NOT NULL,
    [FirmaId] nvarchar(20) NOT NULL,
    [SubeAdi] nvarchar(200) NULL,
    [SubeKodu] nvarchar(50) NULL,
    [AktifMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Subeler] PRIMARY KEY CLUSTERED ([SubeId])
);
END
GO

IF OBJECT_ID(N'dbo.TelemetriGunluk', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[TelemetriGunluk] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Tarih] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [VoltOrt] decimal(10,4) NOT NULL,
    [VoltStd] decimal(10,4) NOT NULL,
    [DevirOrt] decimal(10,4) NOT NULL,
    [DevirStd] decimal(10,4) NOT NULL,
    [BasincOrt] decimal(10,4) NOT NULL,
    [BasincStd] decimal(10,4) NOT NULL,
    [TitresimOrt] decimal(10,4) NOT NULL,
    [TitresimStd] decimal(10,4) NOT NULL,
    [KayitSayisi] int NOT NULL,
    [AnomaliPuani] decimal(10,4) NOT NULL,
    [UretimYogunluguYuzde] decimal(10,4) NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    CONSTRAINT [PK_TelemetriGunluk] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.UretimHatlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UretimHatlari] (
    [HatId] nvarchar(20) NOT NULL,
    [DepartmanId] nvarchar(20) NOT NULL,
    [HatAdi] nvarchar(200) NULL,
    [HatKodu] nvarchar(50) NULL,
    [VardiyaSayisi] int NOT NULL,
    [AktifMi] bit NOT NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_UretimHatlari] PRIMARY KEY CLUSTERED ([HatId])
);
END
GO

IF OBJECT_ID(N'dbo.UyariNedenleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UyariNedenleri] (
    [NedenId] int IDENTITY(1,1) NOT NULL,
    [UyariId] nvarchar(20) NOT NULL,
    [SiraNo] int NOT NULL,
    [RiskNedeni] nvarchar(150) NOT NULL,
    [Aciklama] nvarchar(500) NULL,
    [OnerilenAksiyon] nvarchar(500) NULL,
    CONSTRAINT [PK_UyariNedenleri] PRIMARY KEY CLUSTERED ([NedenId])
);
END
GO

IF OBJECT_ID(N'dbo.Uyarilar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Uyarilar] (
    [UyariId] nvarchar(20) NOT NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [RiskId] nvarchar(20) NULL,
    [AnalizId] int NULL,
    [PlanId] nvarchar(20) NULL,
    [UyariSeviyesi] nvarchar(20) NULL,
    [Baslik] nvarchar(300) NULL,
    [Aciklama] nvarchar(1000) NULL,
    [OnerilenAksiyon] nvarchar(500) NULL,
    [Durum] nvarchar(30) NULL,
    [SonTarih] datetime2 NOT NULL,
    [SorumluPersonelId] nvarchar(20) NULL,
    [VeriKaynagi] nvarchar(50) NULL,
    [KapatmaTarihi] datetime2 NULL,
    [KapatmaNedeni] nvarchar(1000) NULL,
    [IslemYapanPersonelId] nvarchar(20) NULL,
    [IslemSonucu] nvarchar(100) NULL,
    CONSTRAINT [PK_Uyarilar] PRIMARY KEY CLUSTERED ([UyariId])
);
END
GO

IF OBJECT_ID(N'dbo.MakineSayacTanimlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MakineSayacTanimlari] (
    [SayacTanimId] int IDENTITY(1,1) NOT NULL,
    [SayacKodu] nvarchar(30) NOT NULL,
    [SayacAdi] nvarchar(150) NOT NULL,
    [SayacTuru] nvarchar(50) NOT NULL,
    [Birim] nvarchar(30) NOT NULL,
    [Aktif] bit NOT NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_MakineSayacTanimlari] PRIMARY KEY CLUSTERED ([SayacTanimId])
);
END
GO

IF OBJECT_ID(N'dbo.Vardiyalar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Vardiyalar] (
    [VardiyaId] int IDENTITY(1,1) NOT NULL,
    [VardiyaKodu] nvarchar(20) NOT NULL,
    [VardiyaAdi] nvarchar(100) NOT NULL,
    [BaslangicSaati] time NOT NULL,
    [BitisSaati] time NOT NULL,
    [Aktif] bit NOT NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Vardiyalar] PRIMARY KEY CLUSTERED ([VardiyaId])
);
END
GO

IF OBJECT_ID(N'dbo.MakineSayacKayitlari', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MakineSayacKayitlari] (
    [SayacKayitId] bigint IDENTITY(1,1) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [SayacTanimId] int NOT NULL,
    [Tarih] date NOT NULL,
    [VardiyaId] int NOT NULL,
    [BaslangicSayac] decimal(18,3) NOT NULL,
    [BitisSayac] decimal(18,3) NOT NULL,
    [SayacFarki] AS ([BitisSayac]-[BaslangicSayac]) PERSISTED,
    [CevrimSayisi] decimal(18,3) NULL,
    [UretimMiktari] decimal(18,3) NULL,
    [CalismaSuresiDk] int NULL,
    [BostaKalmaSuresiDk] int NULL,
    [UretimSuresiDk] int NULL,
    [VeriKalitesi] decimal(5,2) NULL,
    [VeriKaynagi] nvarchar(50) NOT NULL,
    [Aciklama] nvarchar(1000) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_MakineSayacKayitlari] PRIMARY KEY CLUSTERED ([SayacKayitId])
);
END
GO

IF OBJECT_ID(N'dbo.MakineCalismaTakvimleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MakineCalismaTakvimleri] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [Tarih] date NOT NULL,
    [VardiyaId] int NOT NULL,
    [PlanlananCalismaSuresiDk] int NOT NULL,
    [PlanlananUretimMiktari] decimal(18,3) NULL,
    [Durum] nvarchar(30) NOT NULL DEFAULT 'Planlandi',
    [Aciklama] nvarchar(1000) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_MakineCalismaTakvimleri] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.MakineTransferleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[MakineTransferleri] (
    [TransferId] bigint IDENTITY(1,1) NOT NULL,
    [MakineId] nvarchar(20) NOT NULL,
    [EskiFirmaId] nvarchar(20) NULL,
    [EskiSubeId] nvarchar(20) NULL,
    [EskiDepartmanId] nvarchar(20) NULL,
    [EskiHatId] nvarchar(20) NULL,
    [YeniFirmaId] nvarchar(20) NOT NULL,
    [YeniSubeId] nvarchar(20) NOT NULL,
    [YeniDepartmanId] nvarchar(20) NOT NULL,
    [YeniHatId] nvarchar(20) NOT NULL,
    [TransferTarihi] datetime2 NOT NULL,
    [Gerekce] nvarchar(1000) NOT NULL,
    [IslemiYapanPersonelId] nvarchar(20) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    CONSTRAINT [PK_MakineTransferleri] PRIMARY KEY CLUSTERED ([TransferId])
);
END
GO

IF OBJECT_ID(N'dbo.Ekipler', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[Ekipler] (
    [EkipId] int IDENTITY(1,1) NOT NULL,
    [EkipKodu] nvarchar(30) NOT NULL,
    [EkipAdi] nvarchar(150) NOT NULL,
    [Aktif] bit NOT NULL,
    [Aciklama] nvarchar(500) NULL,
    [OlusturmaTarihi] datetime2 NOT NULL,
    [GuncellemeTarihi] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Ekipler] PRIMARY KEY CLUSTERED ([EkipId])
);
END
GO

IF OBJECT_ID(N'dbo.EkipUyeleri', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[EkipUyeleri] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [EkipId] int NOT NULL,
    [PersonelId] nvarchar(20) NOT NULL,
    [Aktif] bit NOT NULL,
    [BaslangicTarihi] date NOT NULL,
    [BitisTarihi] date NULL,
    CONSTRAINT [PK_EkipUyeleri] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.ArizaDegisenParcalar', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[ArizaDegisenParcalar] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [MudahaleId] int NOT NULL,
    [ParcaId] int NULL,
    [ParcaKodu] nvarchar(20) NULL,
    [Miktar] decimal(18,3) NOT NULL,
    [Aciklama] nvarchar(500) NULL,
    CONSTRAINT [PK_ArizaDegisenParcalar] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.IsEmriDurumGecmisi', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[IsEmriDurumGecmisi] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [IsEmriId] int NOT NULL,
    [EskiDurum] nvarchar(50) NULL,
    [YeniDurum] nvarchar(50) NOT NULL,
    [Tarih] datetime2 NOT NULL,
    [PersonelId] nvarchar(20) NULL,
    [Aciklama] nvarchar(1000) NULL,
    CONSTRAINT [PK_IsEmriDurumGecmisi] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.UyariIslemGecmisi', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[UyariIslemGecmisi] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [UyariId] nvarchar(20) NOT NULL,
    [IslemTuru] nvarchar(50) NOT NULL,
    [EskiDurum] nvarchar(30) NULL,
    [YeniDurum] nvarchar(30) NULL,
    [PersonelId] nvarchar(20) NULL,
    [IslemTarihi] datetime2 NOT NULL,
    [Aciklama] nvarchar(1000) NULL,
    CONSTRAINT [PK_UyariIslemGecmisi] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF OBJECT_ID(N'dbo.BakimPlanDurumGecmisi', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[BakimPlanDurumGecmisi] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [PlanId] nvarchar(20) NOT NULL,
    [EskiDurum] nvarchar(30) NULL,
    [YeniDurum] nvarchar(30) NOT NULL,
    [PersonelId] nvarchar(20) NULL,
    [DegisiklikTarihi] datetime2 NOT NULL,
    [Aciklama] nvarchar(1000) NULL,
    CONSTRAINT [PK_BakimPlanDurumGecmisi] PRIMARY KEY CLUSTERED ([Id])
);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArizaKartlari_MakineId_ArizaTarihi' AND object_id = OBJECT_ID(N'dbo.ArizaKartlari'))
BEGIN
CREATE INDEX [IX_ArizaKartlari_MakineId_ArizaTarihi] ON [dbo].[ArizaKartlari] ([MakineId], [ArizaTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArizaKartlari_PersonelId' AND object_id = OBJECT_ID(N'dbo.ArizaKartlari'))
BEGIN
CREATE INDEX [IX_ArizaKartlari_PersonelId] ON [dbo].[ArizaKartlari] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_ArizaKoduTanimlari' AND object_id = OBJECT_ID(N'dbo.ArizaKoduTanimlari'))
BEGIN
CREATE UNIQUE INDEX [UQ_ArizaKoduTanimlari] ON [dbo].[ArizaKoduTanimlari] ([ArizaKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ArizaMudahaleler_Ariza' AND object_id = OBJECT_ID(N'dbo.ArizaMudahaleler'))
BEGIN
CREATE INDEX [IX_ArizaMudahaleler_Ariza] ON [dbo].[ArizaMudahaleler] ([ArizaId], [MudahaleBaslangic]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimKayitlari_BagliArizaId' AND object_id = OBJECT_ID(N'dbo.BakimKayitlari'))
BEGIN
CREATE INDEX [IX_BakimKayitlari_BagliArizaId] ON [dbo].[BakimKayitlari] ([BagliArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimKayitlari_MakineId_BakimTarihi' AND object_id = OBJECT_ID(N'dbo.BakimKayitlari'))
BEGIN
CREATE INDEX [IX_BakimKayitlari_MakineId_BakimTarihi] ON [dbo].[BakimKayitlari] ([MakineId], [BakimTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimKayitlari_PersonelId' AND object_id = OBJECT_ID(N'dbo.BakimKayitlari'))
BEGIN
CREATE INDEX [IX_BakimKayitlari_PersonelId] ON [dbo].[BakimKayitlari] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BakimKontrolListeleri' AND object_id = OBJECT_ID(N'dbo.BakimKontrolListeleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_BakimKontrolListeleri] ON [dbo].[BakimKontrolListeleri] ([KontrolListesiKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BakimOneriParametreleri' AND object_id = OBJECT_ID(N'dbo.BakimOneriParametreleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_BakimOneriParametreleri] ON [dbo].[BakimOneriParametreleri] ([OneriKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_PlanGorevleri' AND object_id = OBJECT_ID(N'dbo.BakimPlanGorevleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_PlanGorevleri] ON [dbo].[BakimPlanGorevleri] ([PlanId], [SiraNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimPlanlari_MakineId' AND object_id = OBJECT_ID(N'dbo.BakimPlanlari'))
BEGIN
CREATE INDEX [IX_BakimPlanlari_MakineId] ON [dbo].[BakimPlanlari] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimPlanlari_PlanlananTarih' AND object_id = OBJECT_ID(N'dbo.BakimPlanlari'))
BEGIN
CREATE INDEX [IX_BakimPlanlari_PlanlananTarih] ON [dbo].[BakimPlanlari] ([PlanlananTarih]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BakimPlanlari_SorumluPersonelId' AND object_id = OBJECT_ID(N'dbo.BakimPlanlari'))
BEGIN
CREATE INDEX [IX_BakimPlanlari_SorumluPersonelId] ON [dbo].[BakimPlanlari] ([SorumluPersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BakimTuruTanimlari' AND object_id = OBJECT_ID(N'dbo.BakimTuruTanimlari'))
BEGIN
CREATE UNIQUE INDEX [UQ_BakimTuruTanimlari] ON [dbo].[BakimTuruTanimlari] ([BakimTuruKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Departmanlar_SubeId' AND object_id = OBJECT_ID(N'dbo.Departmanlar'))
BEGIN
CREATE INDEX [IX_Departmanlar_SubeId] ON [dbo].[Departmanlar] ([SubeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Departmanlar_SubeKodu' AND object_id = OBJECT_ID(N'dbo.Departmanlar'))
BEGIN
CREATE UNIQUE INDEX [UQ_Departmanlar_SubeKodu] ON [dbo].[Departmanlar] ([SubeId], [DepartmanKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Firmalar_FirmaKodu' AND object_id = OBJECT_ID(N'dbo.Firmalar'))
BEGIN
CREATE UNIQUE INDEX [UQ_Firmalar_FirmaKodu] ON [dbo].[Firmalar] ([FirmaKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GuncelRiskler_MakineId_DegerlendirmeTarihi' AND object_id = OBJECT_ID(N'dbo.GuncelRiskler'))
BEGIN
CREATE INDEX [IX_GuncelRiskler_MakineId_DegerlendirmeTarihi] ON [dbo].[GuncelRiskler] ([MakineId], [DegerlendirmeTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HataKayitlari_BagliArizaId' AND object_id = OBJECT_ID(N'dbo.HataKayitlari'))
BEGIN
CREATE INDEX [IX_HataKayitlari_BagliArizaId] ON [dbo].[HataKayitlari] ([BagliArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HataKayitlari_MakineId_HataTarihi' AND object_id = OBJECT_ID(N'dbo.HataKayitlari'))
BEGIN
CREATE INDEX [IX_HataKayitlari_MakineId_HataTarihi] ON [dbo].[HataKayitlari] ([MakineId], [HataTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_HibritKararParametreleri' AND object_id = OBJECT_ID(N'dbo.HibritKararParametreleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_HibritKararParametreleri] ON [dbo].[HibritKararParametreleri] ([SetKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IsEmirleri_Makine' AND object_id = OBJECT_ID(N'dbo.IsEmirleri'))
BEGIN
CREATE INDEX [IX_IsEmirleri_Makine] ON [dbo].[IsEmirleri] ([MakineId], [PlanlananTarih]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_IsEmirleri' AND object_id = OBJECT_ID(N'dbo.IsEmirleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_IsEmirleri] ON [dbo].[IsEmirleri] ([IsEmriNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KararGecmisi_Makine' AND object_id = OBJECT_ID(N'dbo.KararGecmisi'))
BEGIN
CREATE INDEX [IX_KararGecmisi_Makine] ON [dbo].[KararGecmisi] ([MakineId], [KararTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_KararGecmisi' AND object_id = OBJECT_ID(N'dbo.KararGecmisi'))
BEGIN
CREATE UNIQUE INDEX [UQ_KararGecmisi] ON [dbo].[KararGecmisi] ([KararNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KodListeleri_KodGrubu_Kod' AND object_id = OBJECT_ID(N'dbo.KodListeleri'))
BEGIN
CREATE UNIQUE INDEX [IX_KodListeleri_KodGrubu_Kod] ON [dbo].[KodListeleri] ([KodGrubu], [Kod]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_KontrolMaddeleri' AND object_id = OBJECT_ID(N'dbo.KontrolListesiMaddeleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_KontrolMaddeleri] ON [dbo].[KontrolListesiMaddeleri] ([KontrolListesiId], [SiraNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_MakineBilesenler' AND object_id = OBJECT_ID(N'dbo.MakineBilesenler'))
BEGIN
CREATE UNIQUE INDEX [UQ_MakineBilesenler] ON [dbo].[MakineBilesenler] ([MakineId], [BilesenKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Makineler_DepartmanId' AND object_id = OBJECT_ID(N'dbo.Makineler'))
BEGIN
CREATE INDEX [IX_Makineler_DepartmanId] ON [dbo].[Makineler] ([DepartmanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Makineler_FirmaId' AND object_id = OBJECT_ID(N'dbo.Makineler'))
BEGIN
CREATE INDEX [IX_Makineler_FirmaId] ON [dbo].[Makineler] ([FirmaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Makineler_HatId' AND object_id = OBJECT_ID(N'dbo.Makineler'))
BEGIN
CREATE INDEX [IX_Makineler_HatId] ON [dbo].[Makineler] ([HatId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Makineler_SubeId' AND object_id = OBJECT_ID(N'dbo.Makineler'))
BEGIN
CREATE INDEX [IX_Makineler_SubeId] ON [dbo].[Makineler] ([SubeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Makineler_MakineKodu' AND object_id = OBJECT_ID(N'dbo.Makineler'))
BEGIN
CREATE UNIQUE INDEX [IX_Makineler_MakineKodu] ON [dbo].[Makineler] ([MakineKodu]) WHERE [MakineKodu] IS NOT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Parcalar' AND object_id = OBJECT_ID(N'dbo.Parcalar'))
BEGIN
CREATE UNIQUE INDEX [UQ_Parcalar] ON [dbo].[Parcalar] ([ParcaKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AnalizFaktorleri' AND object_id = OBJECT_ID(N'dbo.RiskAnalizFaktorleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_AnalizFaktorleri] ON [dbo].[RiskAnalizFaktorleri] ([AnalizId], [SiraNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RiskAnalizleri_Makine' AND object_id = OBJECT_ID(N'dbo.RiskAnalizleri'))
BEGIN
CREATE INDEX [IX_RiskAnalizleri_Makine] ON [dbo].[RiskAnalizleri] ([MakineId], [AnalizTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RiskAnalizleri' AND object_id = OBJECT_ID(N'dbo.RiskAnalizleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_RiskAnalizleri] ON [dbo].[RiskAnalizleri] ([AnalizNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_KuralDetaylari' AND object_id = OBJECT_ID(N'dbo.RiskKuralDetaylari'))
BEGIN
CREATE UNIQUE INDEX [UQ_KuralDetaylari] ON [dbo].[RiskKuralDetaylari] ([KuralSetiId], [KuralKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RiskKuralSetleri' AND object_id = OBJECT_ID(N'dbo.RiskKuralSetleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_RiskKuralSetleri] ON [dbo].[RiskKuralSetleri] ([KuralSetiKodu], [Versiyon]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subeler_FirmaId' AND object_id = OBJECT_ID(N'dbo.Subeler'))
BEGIN
CREATE INDEX [IX_Subeler_FirmaId] ON [dbo].[Subeler] ([FirmaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Subeler_FirmaKodu' AND object_id = OBJECT_ID(N'dbo.Subeler'))
BEGIN
CREATE UNIQUE INDEX [UQ_Subeler_FirmaKodu] ON [dbo].[Subeler] ([FirmaId], [SubeKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TelemetriGunluk_MakineId_Tarih' AND object_id = OBJECT_ID(N'dbo.TelemetriGunluk'))
BEGIN
CREATE UNIQUE INDEX [IX_TelemetriGunluk_MakineId_Tarih] ON [dbo].[TelemetriGunluk] ([MakineId], [Tarih]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UretimHatlari_DepartmanId' AND object_id = OBJECT_ID(N'dbo.UretimHatlari'))
BEGIN
CREATE INDEX [IX_UretimHatlari_DepartmanId] ON [dbo].[UretimHatlari] ([DepartmanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_UretimHatlari_DepartmanKodu' AND object_id = OBJECT_ID(N'dbo.UretimHatlari'))
BEGIN
CREATE UNIQUE INDEX [UQ_UretimHatlari_DepartmanKodu] ON [dbo].[UretimHatlari] ([DepartmanId], [HatKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_UyariNedenleri' AND object_id = OBJECT_ID(N'dbo.UyariNedenleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_UyariNedenleri] ON [dbo].[UyariNedenleri] ([UyariId], [SiraNo]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Uyarilar_Durum' AND object_id = OBJECT_ID(N'dbo.Uyarilar'))
BEGIN
CREATE INDEX [IX_Uyarilar_Durum] ON [dbo].[Uyarilar] ([Durum]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Uyarilar_MakineId' AND object_id = OBJECT_ID(N'dbo.Uyarilar'))
BEGIN
CREATE INDEX [IX_Uyarilar_MakineId] ON [dbo].[Uyarilar] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Uyarilar_RiskId' AND object_id = OBJECT_ID(N'dbo.Uyarilar'))
BEGIN
CREATE INDEX [IX_Uyarilar_RiskId] ON [dbo].[Uyarilar] ([RiskId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Uyarilar_SorumluPersonelId' AND object_id = OBJECT_ID(N'dbo.Uyarilar'))
BEGIN
CREATE INDEX [IX_Uyarilar_SorumluPersonelId] ON [dbo].[Uyarilar] ([SorumluPersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_MakineSayacTanimlari_SayacKodu' AND object_id = OBJECT_ID(N'dbo.MakineSayacTanimlari'))
BEGIN
CREATE UNIQUE INDEX [UQ_MakineSayacTanimlari_SayacKodu] ON [dbo].[MakineSayacTanimlari] ([SayacKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Vardiyalar_VardiyaKodu' AND object_id = OBJECT_ID(N'dbo.Vardiyalar'))
BEGIN
CREATE UNIQUE INDEX [UQ_Vardiyalar_VardiyaKodu] ON [dbo].[Vardiyalar] ([VardiyaKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_MakineSayacKayitlari' AND object_id = OBJECT_ID(N'dbo.MakineSayacKayitlari'))
BEGIN
CREATE UNIQUE INDEX [UQ_MakineSayacKayitlari] ON [dbo].[MakineSayacKayitlari] ([MakineId], [SayacTanimId], [Tarih], [VardiyaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_MakineCalismaTakvimleri' AND object_id = OBJECT_ID(N'dbo.MakineCalismaTakvimleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_MakineCalismaTakvimleri] ON [dbo].[MakineCalismaTakvimleri] ([MakineId], [Tarih], [VardiyaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Ekipler_EkipKodu' AND object_id = OBJECT_ID(N'dbo.Ekipler'))
BEGIN
CREATE UNIQUE INDEX [UQ_Ekipler_EkipKodu] ON [dbo].[Ekipler] ([EkipKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_EkipUyeleri' AND object_id = OBJECT_ID(N'dbo.EkipUyeleri'))
BEGIN
CREATE UNIQUE INDEX [UQ_EkipUyeleri] ON [dbo].[EkipUyeleri] ([EkipId], [PersonelId], [BaslangicTarihi]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArizaKartlari_Makineler')
BEGIN
ALTER TABLE [dbo].[ArizaKartlari] ADD CONSTRAINT [FK_ArizaKartlari_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArizaKartlari_Personeller')
BEGIN
ALTER TABLE [dbo].[ArizaKartlari] ADD CONSTRAINT [FK_ArizaKartlari_Personeller] FOREIGN KEY ([PersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArizaMudahaleler_Ariza')
BEGIN
ALTER TABLE [dbo].[ArizaMudahaleler] ADD CONSTRAINT [FK_ArizaMudahaleler_Ariza] FOREIGN KEY ([ArizaId]) REFERENCES [dbo].[ArizaKartlari] ([ArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArizaMudahaleler_Personel')
BEGIN
ALTER TABLE [dbo].[ArizaMudahaleler] ADD CONSTRAINT [FK_ArizaMudahaleler_Personel] FOREIGN KEY ([PersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DegisenParca_Bakim')
BEGIN
ALTER TABLE [dbo].[BakimDegisenParcalar] ADD CONSTRAINT [FK_DegisenParca_Bakim] FOREIGN KEY ([BakimId]) REFERENCES [dbo].[BakimKayitlari] ([BakimId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DegisenParca_Parca')
BEGIN
ALTER TABLE [dbo].[BakimDegisenParcalar] ADD CONSTRAINT [FK_DegisenParca_Parca] FOREIGN KEY ([ParcaKodu]) REFERENCES [dbo].[Parcalar] ([ParcaKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimKayitlari_Ariza')
BEGIN
ALTER TABLE [dbo].[BakimKayitlari] ADD CONSTRAINT [FK_BakimKayitlari_Ariza] FOREIGN KEY ([BagliArizaId]) REFERENCES [dbo].[ArizaKartlari] ([ArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimKayitlari_Makineler')
BEGIN
ALTER TABLE [dbo].[BakimKayitlari] ADD CONSTRAINT [FK_BakimKayitlari_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimKayitlari_Personeller')
BEGIN
ALTER TABLE [dbo].[BakimKayitlari] ADD CONSTRAINT [FK_BakimKayitlari_Personeller] FOREIGN KEY ([PersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KontrolListeleri_BakimTuru')
BEGIN
ALTER TABLE [dbo].[BakimKontrolListeleri] ADD CONSTRAINT [FK_KontrolListeleri_BakimTuru] FOREIGN KEY ([BakimTuruKodu]) REFERENCES [dbo].[BakimTuruTanimlari] ([BakimTuruKodu]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KontrolSonuc_Bakim')
BEGIN
ALTER TABLE [dbo].[BakimKontrolSonuclari] ADD CONSTRAINT [FK_KontrolSonuc_Bakim] FOREIGN KEY ([BakimId]) REFERENCES [dbo].[BakimKayitlari] ([BakimId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KontrolSonuc_Madde')
BEGIN
ALTER TABLE [dbo].[BakimKontrolSonuclari] ADD CONSTRAINT [FK_KontrolSonuc_Madde] FOREIGN KEY ([MaddeId]) REFERENCES [dbo].[KontrolListesiMaddeleri] ([MaddeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlanGorevleri_Plan')
BEGIN
ALTER TABLE [dbo].[BakimPlanGorevleri] ADD CONSTRAINT [FK_PlanGorevleri_Plan] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[BakimPlanlari] ([PlanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PlanGorevleri_Personel')
BEGIN
ALTER TABLE [dbo].[BakimPlanGorevleri] ADD CONSTRAINT [FK_PlanGorevleri_Personel] FOREIGN KEY ([SorumluPersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimPlanlari_Makineler')
BEGIN
ALTER TABLE [dbo].[BakimPlanlari] ADD CONSTRAINT [FK_BakimPlanlari_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimPlanlari_Personeller')
BEGIN
ALTER TABLE [dbo].[BakimPlanlari] ADD CONSTRAINT [FK_BakimPlanlari_Personeller] FOREIGN KEY ([SorumluPersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Departmanlar_Subeler')
BEGIN
ALTER TABLE [dbo].[Departmanlar] ADD CONSTRAINT [FK_Departmanlar_Subeler] FOREIGN KEY ([SubeId]) REFERENCES [dbo].[Subeler] ([SubeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GuncelRiskler_Makineler')
BEGIN
ALTER TABLE [dbo].[GuncelRiskler] ADD CONSTRAINT [FK_GuncelRiskler_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HataKayitlari_Ariza')
BEGIN
ALTER TABLE [dbo].[HataKayitlari] ADD CONSTRAINT [FK_HataKayitlari_Ariza] FOREIGN KEY ([BagliArizaId]) REFERENCES [dbo].[ArizaKartlari] ([ArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HataKayitlari_Makineler')
BEGIN
ALTER TABLE [dbo].[HataKayitlari] ADD CONSTRAINT [FK_HataKayitlari_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Hibrit_KuralSeti')
BEGIN
ALTER TABLE [dbo].[HibritKararParametreleri] ADD CONSTRAINT [FK_Hibrit_KuralSeti] FOREIGN KEY ([KuralSetiId]) REFERENCES [dbo].[RiskKuralSetleri] ([KuralSetiId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IsEmirleri_Ariza')
BEGIN
ALTER TABLE [dbo].[IsEmirleri] ADD CONSTRAINT [FK_IsEmirleri_Ariza] FOREIGN KEY ([ArizaId]) REFERENCES [dbo].[ArizaKartlari] ([ArizaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IsEmirleri_Makine')
BEGIN
ALTER TABLE [dbo].[IsEmirleri] ADD CONSTRAINT [FK_IsEmirleri_Makine] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IsEmirleri_Plan')
BEGIN
ALTER TABLE [dbo].[IsEmirleri] ADD CONSTRAINT [FK_IsEmirleri_Plan] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[BakimPlanlari] ([PlanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IsEmirleri_Personel')
BEGIN
ALTER TABLE [dbo].[IsEmirleri] ADD CONSTRAINT [FK_IsEmirleri_Personel] FOREIGN KEY ([SorumluPersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KararGecmisi_Analiz')
BEGIN
ALTER TABLE [dbo].[KararGecmisi] ADD CONSTRAINT [FK_KararGecmisi_Analiz] FOREIGN KEY ([AnalizId]) REFERENCES [dbo].[RiskAnalizleri] ([AnalizId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KararGecmisi_Personel')
BEGIN
ALTER TABLE [dbo].[KararGecmisi] ADD CONSTRAINT [FK_KararGecmisi_Personel] FOREIGN KEY ([KararVerenPersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KararGecmisi_Makine')
BEGIN
ALTER TABLE [dbo].[KararGecmisi] ADD CONSTRAINT [FK_KararGecmisi_Makine] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KontrolMaddeleri_Liste')
BEGIN
ALTER TABLE [dbo].[KontrolListesiMaddeleri] ADD CONSTRAINT [FK_KontrolMaddeleri_Liste] FOREIGN KEY ([KontrolListesiId]) REFERENCES [dbo].[BakimKontrolListeleri] ([KontrolListesiId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineBilesenler_Makineler')
BEGIN
ALTER TABLE [dbo].[MakineBilesenler] ADD CONSTRAINT [FK_MakineBilesenler_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Makineler_Departmanlar')
BEGIN
ALTER TABLE [dbo].[Makineler] ADD CONSTRAINT [FK_Makineler_Departmanlar] FOREIGN KEY ([DepartmanId]) REFERENCES [dbo].[Departmanlar] ([DepartmanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Makineler_Firmalar')
BEGIN
ALTER TABLE [dbo].[Makineler] ADD CONSTRAINT [FK_Makineler_Firmalar] FOREIGN KEY ([FirmaId]) REFERENCES [dbo].[Firmalar] ([FirmaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Makineler_UretimHatlari')
BEGIN
ALTER TABLE [dbo].[Makineler] ADD CONSTRAINT [FK_Makineler_UretimHatlari] FOREIGN KEY ([HatId]) REFERENCES [dbo].[UretimHatlari] ([HatId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Makineler_Subeler')
BEGIN
ALTER TABLE [dbo].[Makineler] ADD CONSTRAINT [FK_Makineler_Subeler] FOREIGN KEY ([SubeId]) REFERENCES [dbo].[Subeler] ([SubeId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AnalizFaktorleri_Analiz')
BEGIN
ALTER TABLE [dbo].[RiskAnalizFaktorleri] ADD CONSTRAINT [FK_AnalizFaktorleri_Analiz] FOREIGN KEY ([AnalizId]) REFERENCES [dbo].[RiskAnalizleri] ([AnalizId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RiskAnalizleri_KuralSeti')
BEGIN
ALTER TABLE [dbo].[RiskAnalizleri] ADD CONSTRAINT [FK_RiskAnalizleri_KuralSeti] FOREIGN KEY ([KuralSetiId]) REFERENCES [dbo].[RiskKuralSetleri] ([KuralSetiId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RiskAnalizleri_Makine')
BEGIN
ALTER TABLE [dbo].[RiskAnalizleri] ADD CONSTRAINT [FK_RiskAnalizleri_Makine] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KuralDetaylari_Set')
BEGIN
ALTER TABLE [dbo].[RiskKuralDetaylari] ADD CONSTRAINT [FK_KuralDetaylari_Set] FOREIGN KEY ([KuralSetiId]) REFERENCES [dbo].[RiskKuralSetleri] ([KuralSetiId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Subeler_Firmalar')
BEGIN
ALTER TABLE [dbo].[Subeler] ADD CONSTRAINT [FK_Subeler_Firmalar] FOREIGN KEY ([FirmaId]) REFERENCES [dbo].[Firmalar] ([FirmaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TelemetriGunluk_Makineler')
BEGIN
ALTER TABLE [dbo].[TelemetriGunluk] ADD CONSTRAINT [FK_TelemetriGunluk_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UretimHatlari_Departmanlar')
BEGIN
ALTER TABLE [dbo].[UretimHatlari] ADD CONSTRAINT [FK_UretimHatlari_Departmanlar] FOREIGN KEY ([DepartmanId]) REFERENCES [dbo].[Departmanlar] ([DepartmanId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UyariNedenleri_Uyari')
BEGIN
ALTER TABLE [dbo].[UyariNedenleri] ADD CONSTRAINT [FK_UyariNedenleri_Uyari] FOREIGN KEY ([UyariId]) REFERENCES [dbo].[Uyarilar] ([UyariId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Uyarilar_Makineler')
BEGIN
ALTER TABLE [dbo].[Uyarilar] ADD CONSTRAINT [FK_Uyarilar_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Uyarilar_GuncelRiskler')
BEGIN
ALTER TABLE [dbo].[Uyarilar] ADD CONSTRAINT [FK_Uyarilar_GuncelRiskler] FOREIGN KEY ([RiskId]) REFERENCES [dbo].[GuncelRiskler] ([RiskId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Uyarilar_Personeller')
BEGIN
ALTER TABLE [dbo].[Uyarilar] ADD CONSTRAINT [FK_Uyarilar_Personeller] FOREIGN KEY ([SorumluPersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineSayacKayitlari_Makineler')
BEGIN
ALTER TABLE [dbo].[MakineSayacKayitlari] ADD CONSTRAINT [FK_MakineSayacKayitlari_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineSayacKayitlari_SayacTanimlari')
BEGIN
ALTER TABLE [dbo].[MakineSayacKayitlari] ADD CONSTRAINT [FK_MakineSayacKayitlari_SayacTanimlari] FOREIGN KEY ([SayacTanimId]) REFERENCES [dbo].[MakineSayacTanimlari] ([SayacTanimId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineSayacKayitlari_Vardiyalar')
BEGIN
ALTER TABLE [dbo].[MakineSayacKayitlari] ADD CONSTRAINT [FK_MakineSayacKayitlari_Vardiyalar] FOREIGN KEY ([VardiyaId]) REFERENCES [dbo].[Vardiyalar] ([VardiyaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineCalismaTakvimleri_Makineler')
BEGIN
ALTER TABLE [dbo].[MakineCalismaTakvimleri] ADD CONSTRAINT [FK_MakineCalismaTakvimleri_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineCalismaTakvimleri_Vardiyalar')
BEGIN
ALTER TABLE [dbo].[MakineCalismaTakvimleri] ADD CONSTRAINT [FK_MakineCalismaTakvimleri_Vardiyalar] FOREIGN KEY ([VardiyaId]) REFERENCES [dbo].[Vardiyalar] ([VardiyaId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MakineTransferleri_Makineler')
BEGIN
ALTER TABLE [dbo].[MakineTransferleri] ADD CONSTRAINT [FK_MakineTransferleri_Makineler] FOREIGN KEY ([MakineId]) REFERENCES [dbo].[Makineler] ([MakineId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EkipUyeleri_Ekipler')
BEGIN
ALTER TABLE [dbo].[EkipUyeleri] ADD CONSTRAINT [FK_EkipUyeleri_Ekipler] FOREIGN KEY ([EkipId]) REFERENCES [dbo].[Ekipler] ([EkipId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_EkipUyeleri_Personeller')
BEGIN
ALTER TABLE [dbo].[EkipUyeleri] ADD CONSTRAINT [FK_EkipUyeleri_Personeller] FOREIGN KEY ([PersonelId]) REFERENCES [dbo].[Personeller] ([PersonelId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArizaDegisenParcalar_Mudahaleler')
BEGIN
ALTER TABLE [dbo].[ArizaDegisenParcalar] ADD CONSTRAINT [FK_ArizaDegisenParcalar_Mudahaleler] FOREIGN KEY ([MudahaleId]) REFERENCES [dbo].[ArizaMudahaleler] ([MudahaleId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_IsEmriDurumGecmisi_IsEmirleri')
BEGIN
ALTER TABLE [dbo].[IsEmriDurumGecmisi] ADD CONSTRAINT [FK_IsEmriDurumGecmisi_IsEmirleri] FOREIGN KEY ([IsEmriId]) REFERENCES [dbo].[IsEmirleri] ([IsEmriId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UyariIslemGecmisi_Uyarilar')
BEGIN
ALTER TABLE [dbo].[UyariIslemGecmisi] ADD CONSTRAINT [FK_UyariIslemGecmisi_Uyarilar] FOREIGN KEY ([UyariId]) REFERENCES [dbo].[Uyarilar] ([UyariId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BakimPlanDurumGecmisi_BakimPlanlari')
BEGIN
ALTER TABLE [dbo].[BakimPlanDurumGecmisi] ADD CONSTRAINT [FK_BakimPlanDurumGecmisi_BakimPlanlari] FOREIGN KEY ([PlanId]) REFERENCES [dbo].[BakimPlanlari] ([PlanId]);
END
GO
