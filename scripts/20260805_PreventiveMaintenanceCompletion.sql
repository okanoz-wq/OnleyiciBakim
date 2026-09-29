/*
    Önleyici Bakım Yönetim Sistemi - güvenli şema tamamlama betiği
    Tarih: 2026-08-05

    Özellikler:
    - İdempotenttir; aynı veritabanında tekrar çalıştırılabilir.
    - Veritabanı veya mevcut tablo silmez.
    - Eski metin alanlarını korur ve yeni FK alanlarını nullable ekler.
    - Çalıştırmadan önce tam SQL Server yedeği alınmalıdır.
*/
USE [BakimYonetimiDb];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /* Organizasyon kartları: yerel CRUD, audit ve soft-delete desteği. */
    IF COL_LENGTH('dbo.Firmalar', 'Aciklama') IS NULL ALTER TABLE dbo.Firmalar ADD Aciklama nvarchar(500) NULL;
    IF COL_LENGTH('dbo.Firmalar', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.Firmalar ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Firmalar_OlusturmaTarihi DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.Firmalar', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.Firmalar ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.Firmalar', 'RowVersion') IS NULL ALTER TABLE dbo.Firmalar ADD RowVersion rowversion;

    IF COL_LENGTH('dbo.Subeler', 'Aciklama') IS NULL ALTER TABLE dbo.Subeler ADD Aciklama nvarchar(500) NULL;
    IF COL_LENGTH('dbo.Subeler', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.Subeler ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Subeler_OlusturmaTarihi DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.Subeler', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.Subeler ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.Subeler', 'RowVersion') IS NULL ALTER TABLE dbo.Subeler ADD RowVersion rowversion;

    IF COL_LENGTH('dbo.Departmanlar', 'Aciklama') IS NULL ALTER TABLE dbo.Departmanlar ADD Aciklama nvarchar(500) NULL;
    IF COL_LENGTH('dbo.Departmanlar', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.Departmanlar ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Departmanlar_OlusturmaTarihi DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.Departmanlar', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.Departmanlar ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.Departmanlar', 'RowVersion') IS NULL ALTER TABLE dbo.Departmanlar ADD RowVersion rowversion;

    /* UretimHatlari dokümandaki İş Merkezi kavramının mevcut karşılığıdır. */
    IF COL_LENGTH('dbo.UretimHatlari', 'Aciklama') IS NULL ALTER TABLE dbo.UretimHatlari ADD Aciklama nvarchar(500) NULL;
    IF COL_LENGTH('dbo.UretimHatlari', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.UretimHatlari ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_UretimHatlari_OlusturmaTarihi DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.UretimHatlari', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.UretimHatlari ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.UretimHatlari', 'RowVersion') IS NULL ALTER TABLE dbo.UretimHatlari ADD RowVersion rowversion;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Firmalar') AND name='UQ_Firmalar_FirmaKodu')
        CREATE UNIQUE INDEX UQ_Firmalar_FirmaKodu ON dbo.Firmalar(FirmaKodu) WHERE FirmaKodu IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Subeler') AND name='UQ_Subeler_FirmaKodu')
        CREATE UNIQUE INDEX UQ_Subeler_FirmaKodu ON dbo.Subeler(FirmaId,SubeKodu) WHERE SubeKodu IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Departmanlar') AND name='UQ_Departmanlar_SubeKodu')
        CREATE UNIQUE INDEX UQ_Departmanlar_SubeKodu ON dbo.Departmanlar(SubeId,DepartmanKodu) WHERE DepartmanKodu IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.UretimHatlari') AND name='UQ_UretimHatlari_DepartmanKodu')
        CREATE UNIQUE INDEX UQ_UretimHatlari_DepartmanKodu ON dbo.UretimHatlari(DepartmanId,HatKodu) WHERE HatKodu IS NOT NULL;

    /* Makine kartı. Eski Yas ve KurulumYili alanları geriye uyumluluk için korunur. */
    IF COL_LENGTH('dbo.Makineler', 'MakineTuru') IS NULL ALTER TABLE dbo.Makineler ADD MakineTuru nvarchar(100) NULL;
    IF COL_LENGTH('dbo.Makineler', 'Marka') IS NULL ALTER TABLE dbo.Makineler ADD Marka nvarchar(100) NULL;
    IF COL_LENGTH('dbo.Makineler', 'SeriNo') IS NULL ALTER TABLE dbo.Makineler ADD SeriNo nvarchar(100) NULL;
    IF COL_LENGTH('dbo.Makineler', 'KurulumTarihi') IS NULL ALTER TABLE dbo.Makineler ADD KurulumTarihi date NULL;
    IF COL_LENGTH('dbo.Makineler', 'PeriyotTuru') IS NULL ALTER TABLE dbo.Makineler ADD PeriyotTuru nvarchar(50) NULL;
    IF COL_LENGTH('dbo.Makineler', 'PeriyotDegeri') IS NULL ALTER TABLE dbo.Makineler ADD PeriyotDegeri decimal(12,2) NULL;
    IF COL_LENGTH('dbo.Makineler', 'PeriyotBirimi') IS NULL ALTER TABLE dbo.Makineler ADD PeriyotBirimi nvarchar(30) NULL;
    IF COL_LENGTH('dbo.Makineler', 'Aktif') IS NULL ALTER TABLE dbo.Makineler ADD Aktif bit NOT NULL CONSTRAINT DF_Makineler_Aktif DEFAULT (1);
    IF COL_LENGTH('dbo.Makineler', 'Aciklama') IS NULL ALTER TABLE dbo.Makineler ADD Aciklama nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.Makineler', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.Makineler ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Makineler_OlusturmaTarihi DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.Makineler', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.Makineler ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.Makineler', 'RowVersion') IS NULL ALTER TABLE dbo.Makineler ADD RowVersion rowversion;

    EXEC(N'UPDATE dbo.Makineler
       SET KurulumTarihi = DATEFROMPARTS(CASE WHEN KurulumYili BETWEEN 1900 AND YEAR(GETDATE()) THEN KurulumYili ELSE YEAR(GETDATE()) END, 1, 1)
     WHERE KurulumTarihi IS NULL;');
    EXEC(N'UPDATE dbo.Makineler SET Aktif = CASE WHEN UPPER(ISNULL(Durum, '''')) IN (''PASIF'',''PASİF'') THEN 0 ELSE 1 END;');

    ALTER TABLE dbo.Makineler ALTER COLUMN KaynakMakineId int NULL;
    ALTER TABLE dbo.Makineler ALTER COLUMN SonBakimTarihi datetime2(7) NULL;
    ALTER TABLE dbo.Makineler ALTER COLUMN GuncelRiskPuani decimal(5,2) NULL;
    IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Makineler') AND name='MakineKodu' AND is_nullable=1)
    BEGIN
        IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Makineler') AND name='IX_Makineler_MakineKodu')
            DROP INDEX IX_Makineler_MakineKodu ON dbo.Makineler;
        ALTER TABLE dbo.Makineler ALTER COLUMN MakineKodu nvarchar(50) NOT NULL;
        CREATE UNIQUE INDEX IX_Makineler_MakineKodu ON dbo.Makineler(MakineKodu);
    END;
    ALTER TABLE dbo.Makineler ALTER COLUMN MakineAdi nvarchar(200) NOT NULL;

    IF OBJECT_ID('CK_Makineler_KurulumTarihi', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.Makineler ADD CONSTRAINT CK_Makineler_KurulumTarihi CHECK (KurulumTarihi IS NULL OR KurulumTarihi <= CONVERT(date, GETDATE()));');
    IF OBJECT_ID('CK_Makineler_PeriyotDegeri', 'C') IS NULL
        EXEC(N'ALTER TABLE dbo.Makineler ADD CONSTRAINT CK_Makineler_PeriyotDegeri CHECK (PeriyotDegeri IS NULL OR PeriyotDegeri >= 0);');

    /* Sayaç, vardiya ve çalışma kayıtları. */
    IF OBJECT_ID('dbo.MakineSayacTanimlari', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MakineSayacTanimlari(
            SayacTanimId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MakineSayacTanimlari PRIMARY KEY,
            SayacKodu nvarchar(30) NOT NULL,
            SayacAdi nvarchar(150) NOT NULL,
            SayacTuru nvarchar(50) NOT NULL,
            Birim nvarchar(30) NOT NULL,
            Aktif bit NOT NULL CONSTRAINT DF_MakineSayacTanimlari_Aktif DEFAULT (1),
            Aciklama nvarchar(500) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_MakineSayacTanimlari_Olusturma DEFAULT SYSUTCDATETIME(),
            GuncellemeTarihi datetime2(0) NULL,
            RowVersion rowversion,
            CONSTRAINT UQ_MakineSayacTanimlari_SayacKodu UNIQUE(SayacKodu)
        );
    END;

    IF OBJECT_ID('dbo.Vardiyalar', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Vardiyalar(
            VardiyaId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vardiyalar PRIMARY KEY,
            VardiyaKodu nvarchar(20) NOT NULL,
            VardiyaAdi nvarchar(100) NOT NULL,
            BaslangicSaati time(0) NOT NULL,
            BitisSaati time(0) NOT NULL,
            Aktif bit NOT NULL CONSTRAINT DF_Vardiyalar_Aktif DEFAULT (1),
            Aciklama nvarchar(500) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Vardiyalar_Olusturma DEFAULT SYSUTCDATETIME(),
            GuncellemeTarihi datetime2(0) NULL,
            RowVersion rowversion,
            CONSTRAINT UQ_Vardiyalar_VardiyaKodu UNIQUE(VardiyaKodu)
        );
    END;

    IF OBJECT_ID('dbo.MakineSayacKayitlari', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MakineSayacKayitlari(
            SayacKayitId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MakineSayacKayitlari PRIMARY KEY,
            MakineId nvarchar(20) NOT NULL,
            SayacTanimId int NOT NULL,
            Tarih date NOT NULL,
            VardiyaId int NOT NULL,
            BaslangicSayac decimal(18,3) NOT NULL,
            BitisSayac decimal(18,3) NOT NULL,
            SayacFarki AS (BitisSayac-BaslangicSayac) PERSISTED,
            CevrimSayisi decimal(18,3) NULL,
            UretimMiktari decimal(18,3) NULL,
            CalismaSuresiDk int NULL,
            BostaKalmaSuresiDk int NULL,
            UretimSuresiDk int NULL,
            VeriKalitesi decimal(5,2) NULL,
            VeriKaynagi nvarchar(50) NOT NULL CONSTRAINT DF_MakineSayacKayitlari_Kaynak DEFAULT ('LOCAL'),
            Aciklama nvarchar(1000) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_MakineSayacKayitlari_Olusturma DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_MakineSayacKayitlari_Makine FOREIGN KEY(MakineId) REFERENCES dbo.Makineler(MakineId),
            CONSTRAINT FK_MakineSayacKayitlari_Tanim FOREIGN KEY(SayacTanimId) REFERENCES dbo.MakineSayacTanimlari(SayacTanimId),
            CONSTRAINT FK_MakineSayacKayitlari_Vardiya FOREIGN KEY(VardiyaId) REFERENCES dbo.Vardiyalar(VardiyaId),
            CONSTRAINT UQ_MakineSayacKayitlari UNIQUE(MakineId, SayacTanimId, Tarih, VardiyaId),
            CONSTRAINT CK_MakineSayacKayitlari_Sayac CHECK(BitisSayac >= BaslangicSayac),
            CONSTRAINT CK_MakineSayacKayitlari_Sure CHECK(
                (CalismaSuresiDk IS NULL OR CalismaSuresiDk >= 0) AND
                (BostaKalmaSuresiDk IS NULL OR BostaKalmaSuresiDk >= 0) AND
                (UretimSuresiDk IS NULL OR UretimSuresiDk >= 0)),
            CONSTRAINT CK_MakineSayacKayitlari_Kalite CHECK(VeriKalitesi IS NULL OR VeriKalitesi BETWEEN 0 AND 100)
        );
        CREATE INDEX IX_MakineSayacKayitlari_Tarih ON dbo.MakineSayacKayitlari(MakineId, Tarih DESC);
    END;

    IF OBJECT_ID('dbo.MakineCalismaTakvimleri', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MakineCalismaTakvimleri(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MakineCalismaTakvimleri PRIMARY KEY,
            MakineId nvarchar(20) NOT NULL,
            Tarih date NOT NULL,
            VardiyaId int NOT NULL,
            PlanlananCalismaSuresiDk int NOT NULL,
            PlanlananUretimMiktari decimal(18,3) NULL,
            Durum nvarchar(30) NOT NULL CONSTRAINT DF_MakineCalismaTakvimleri_Durum DEFAULT (N'Planlandı'),
            Aciklama nvarchar(1000) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_MakineCalismaTakvimleri_Olusturma DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_MakineCalismaTakvimleri_Makine FOREIGN KEY(MakineId) REFERENCES dbo.Makineler(MakineId),
            CONSTRAINT FK_MakineCalismaTakvimleri_Vardiya FOREIGN KEY(VardiyaId) REFERENCES dbo.Vardiyalar(VardiyaId),
            CONSTRAINT UQ_MakineCalismaTakvimleri UNIQUE(MakineId, Tarih, VardiyaId),
            CONSTRAINT CK_MakineCalismaTakvimleri_Sure CHECK(PlanlananCalismaSuresiDk >= 0),
            CONSTRAINT CK_MakineCalismaTakvimleri_Uretim CHECK(PlanlananUretimMiktari IS NULL OR PlanlananUretimMiktari >= 0)
        );
    END;

    IF OBJECT_ID('dbo.MakineTransferleri', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.MakineTransferleri(
            TransferId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MakineTransferleri PRIMARY KEY,
            MakineId nvarchar(20) NOT NULL,
            EskiFirmaId nvarchar(20) NULL, EskiSubeId nvarchar(20) NULL, EskiDepartmanId nvarchar(20) NULL, EskiHatId nvarchar(20) NULL,
            YeniFirmaId nvarchar(20) NOT NULL, YeniSubeId nvarchar(20) NOT NULL, YeniDepartmanId nvarchar(20) NOT NULL, YeniHatId nvarchar(20) NOT NULL,
            TransferTarihi datetime2(0) NOT NULL,
            Gerekce nvarchar(1000) NOT NULL,
            IslemiYapanPersonelId nvarchar(20) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_MakineTransferleri_Olusturma DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_MakineTransferleri_Makine FOREIGN KEY(MakineId) REFERENCES dbo.Makineler(MakineId),
            CONSTRAINT FK_MakineTransferleri_YeniFirma FOREIGN KEY(YeniFirmaId) REFERENCES dbo.Firmalar(FirmaId),
            CONSTRAINT FK_MakineTransferleri_YeniSube FOREIGN KEY(YeniSubeId) REFERENCES dbo.Subeler(SubeId),
            CONSTRAINT FK_MakineTransferleri_YeniDepartman FOREIGN KEY(YeniDepartmanId) REFERENCES dbo.Departmanlar(DepartmanId),
            CONSTRAINT FK_MakineTransferleri_YeniHat FOREIGN KEY(YeniHatId) REFERENCES dbo.UretimHatlari(HatId),
            CONSTRAINT FK_MakineTransferleri_Personel FOREIGN KEY(IslemiYapanPersonelId) REFERENCES dbo.Personeller(PersonelId)
        );
        CREATE INDEX IX_MakineTransferleri_MakineTarih ON dbo.MakineTransferleri(MakineId, TransferTarihi DESC);
    END;

    /* Ekip yönetimi. */
    IF OBJECT_ID('dbo.Ekipler', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Ekipler(
            EkipId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ekipler PRIMARY KEY,
            EkipKodu nvarchar(30) NOT NULL,
            EkipAdi nvarchar(150) NOT NULL,
            Aktif bit NOT NULL CONSTRAINT DF_Ekipler_Aktif DEFAULT(1),
            Aciklama nvarchar(500) NULL,
            OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_Ekipler_Olusturma DEFAULT SYSUTCDATETIME(),
            GuncellemeTarihi datetime2(0) NULL,
            RowVersion rowversion,
            CONSTRAINT UQ_Ekipler_EkipKodu UNIQUE(EkipKodu)
        );
    END;

    IF OBJECT_ID('dbo.EkipUyeleri', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.EkipUyeleri(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EkipUyeleri PRIMARY KEY,
            EkipId int NOT NULL,
            PersonelId nvarchar(20) NOT NULL,
            Aktif bit NOT NULL CONSTRAINT DF_EkipUyeleri_Aktif DEFAULT(1),
            BaslangicTarihi date NOT NULL CONSTRAINT DF_EkipUyeleri_Baslangic DEFAULT(CONVERT(date, GETDATE())),
            BitisTarihi date NULL,
            CONSTRAINT FK_EkipUyeleri_Ekip FOREIGN KEY(EkipId) REFERENCES dbo.Ekipler(EkipId),
            CONSTRAINT FK_EkipUyeleri_Personel FOREIGN KEY(PersonelId) REFERENCES dbo.Personeller(PersonelId),
            CONSTRAINT UQ_EkipUyeleri UNIQUE(EkipId, PersonelId, BaslangicTarihi),
            CONSTRAINT CK_EkipUyeleri_Tarih CHECK(BitisTarihi IS NULL OR BitisTarihi >= BaslangicTarihi)
        );
    END;

    /* Mevcut tanım ve parametre tablolarının tamamlanması. */
    IF COL_LENGTH('dbo.BakimKontrolListeleri', 'MakineTuru') IS NULL ALTER TABLE dbo.BakimKontrolListeleri ADD MakineTuru nvarchar(100) NULL;
    IF COL_LENGTH('dbo.BakimKontrolListeleri', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.BakimKontrolListeleri ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_BakimKontrolListeleri_Olusturma DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.BakimKontrolListeleri', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.BakimKontrolListeleri ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.HibritKararParametreleri', 'BuyukFarktaEngelle') IS NULL ALTER TABLE dbo.HibritKararParametreleri ADD BuyukFarktaEngelle bit NOT NULL CONSTRAINT DF_HibritKarar_BuyukFark DEFAULT(0);
    IF COL_LENGTH('dbo.BakimOneriParametreleri', 'IslemGerekmiyorSecenegi') IS NULL ALTER TABLE dbo.BakimOneriParametreleri ADD IslemGerekmiyorSecenegi bit NOT NULL CONSTRAINT DF_BakimOneri_IslemGerekmiyor DEFAULT(1);
    IF COL_LENGTH('dbo.BakimOneriParametreleri', 'VarsayilanKarar') IS NULL ALTER TABLE dbo.BakimOneriParametreleri ADD VarsayilanKarar nvarchar(50) NULL;
    IF COL_LENGTH('dbo.BakimOneriParametreleri', 'MaxDahil') IS NULL ALTER TABLE dbo.BakimOneriParametreleri ADD MaxDahil bit NOT NULL CONSTRAINT DF_BakimOneri_MaxDahil DEFAULT(0);
    IF OBJECT_ID('CK_OneriPuanAraligi', 'C') IS NOT NULL ALTER TABLE dbo.BakimOneriParametreleri DROP CONSTRAINT CK_OneriPuanAraligi;
    ALTER TABLE dbo.BakimOneriParametreleri ALTER COLUMN MinPuan decimal(5,2) NOT NULL;
    ALTER TABLE dbo.BakimOneriParametreleri ALTER COLUMN MaxPuan decimal(5,2) NOT NULL;

    IF OBJECT_ID('CK_HibritKararParametreleri_Agirlik', 'C') IS NULL
        ALTER TABLE dbo.HibritKararParametreleri ADD CONSTRAINT CK_HibritKararParametreleri_Agirlik CHECK(MlAgirligi BETWEEN 0 AND 100 AND KuralAgirligi BETWEEN 0 AND 100 AND MlAgirligi + KuralAgirligi = 100);
    IF OBJECT_ID('CK_BakimOneriParametreleri_Puan', 'C') IS NULL
        ALTER TABLE dbo.BakimOneriParametreleri ADD CONSTRAINT CK_BakimOneriParametreleri_Puan CHECK(MinPuan >= 0 AND MaxPuan <= 100 AND MinPuan < MaxPuan);

    /* Arıza bildirimi / müdahale / kapatma. İlk bildirimde bilinmeyen süre ve maliyetler nullable'dır. */
    IF COL_LENGTH('dbo.ArizaKartlari', 'ArizaKoduId') IS NULL ALTER TABLE dbo.ArizaKartlari ADD ArizaKoduId int NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'VardiyaId') IS NULL ALTER TABLE dbo.ArizaKartlari ADD VardiyaId int NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'UrunStokKodu') IS NULL ALTER TABLE dbo.ArizaKartlari ADD UrunStokKodu nvarchar(100) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'IlkBelirti') IS NULL ALTER TABLE dbo.ArizaKartlari ADD IlkBelirti nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'UretimDurduMu') IS NULL ALTER TABLE dbo.ArizaKartlari ADD UretimDurduMu bit NOT NULL CONSTRAINT DF_ArizaKartlari_UretimDurdu DEFAULT(0);
    IF COL_LENGTH('dbo.ArizaKartlari', 'TahminiDurusSuresiDk') IS NULL ALTER TABLE dbo.ArizaKartlari ADD TahminiDurusSuresiDk int NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'BildirenPersonelId') IS NULL ALTER TABLE dbo.ArizaKartlari ADD BildirenPersonelId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'ArizaBitisTarihi') IS NULL ALTER TABLE dbo.ArizaKartlari ADD ArizaBitisTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'UretimKaybiMiktari') IS NULL ALTER TABLE dbo.ArizaKartlari ADD UretimKaybiMiktari decimal(18,3) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'UretimKaybiBirimi') IS NULL ALTER TABLE dbo.ArizaKartlari ADD UretimKaybiBirimi nvarchar(30) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'Aktif') IS NULL ALTER TABLE dbo.ArizaKartlari ADD Aktif bit NOT NULL CONSTRAINT DF_ArizaKartlari_Aktif DEFAULT(1);
    IF COL_LENGTH('dbo.ArizaKartlari', 'KapanisPersonelId') IS NULL ALTER TABLE dbo.ArizaKartlari ADD KapanisPersonelId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'KapanisTarihi') IS NULL ALTER TABLE dbo.ArizaKartlari ADD KapanisTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'CozumAciklamasi') IS NULL ALTER TABLE dbo.ArizaKartlari ADD CozumAciklamasi nvarchar(2000) NULL;
    IF COL_LENGTH('dbo.ArizaKartlari', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.ArizaKartlari ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_ArizaKartlari_Olusturma DEFAULT SYSUTCDATETIME();
    ALTER TABLE dbo.ArizaKartlari ALTER COLUMN IlkMudahaleDk int NULL;
    ALTER TABLE dbo.ArizaKartlari ALTER COLUMN MudahaleSuresiDk int NULL;
    ALTER TABLE dbo.ArizaKartlari ALTER COLUMN DurusSuresiDk int NULL;
    ALTER TABLE dbo.ArizaKartlari ALTER COLUMN TahminiMaliyetTl decimal(18,2) NULL;

    IF OBJECT_ID('dbo.ArizaDegisenParcalar', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ArizaDegisenParcalar(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArizaDegisenParcalar PRIMARY KEY,
            MudahaleId int NOT NULL,
            ParcaId int NULL,
            ParcaKodu nvarchar(20) NULL,
            Miktar decimal(18,3) NOT NULL,
            Aciklama nvarchar(500) NULL,
            CONSTRAINT FK_ArizaDegisenParcalar_Mudahale FOREIGN KEY(MudahaleId) REFERENCES dbo.ArizaMudahaleler(MudahaleId),
            CONSTRAINT FK_ArizaDegisenParcalar_Parca FOREIGN KEY(ParcaId) REFERENCES dbo.Parcalar(ParcaId),
            CONSTRAINT CK_ArizaDegisenParcalar_Miktar CHECK(Miktar > 0),
            CONSTRAINT CK_ArizaDegisenParcalar_Parca CHECK(ParcaId IS NOT NULL OR NULLIF(LTRIM(RTRIM(ParcaKodu)), '') IS NOT NULL)
        );
    END;

    /* Bakım planı ve gerçekleşme ilişkileri. */
    IF COL_LENGTH('dbo.BakimPlanlari', 'AnalizId') IS NULL ALTER TABLE dbo.BakimPlanlari ADD AnalizId int NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'UyariId') IS NULL ALTER TABLE dbo.BakimPlanlari ADD UyariId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'OnerilenTarih') IS NULL ALTER TABLE dbo.BakimPlanlari ADD OnerilenTarih date NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'BakimTuruId') IS NULL ALTER TABLE dbo.BakimPlanlari ADD BakimTuruId int NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'SorumluEkipId') IS NULL ALTER TABLE dbo.BakimPlanlari ADD SorumluEkipId int NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'OnayDurumu') IS NULL ALTER TABLE dbo.BakimPlanlari ADD OnayDurumu nvarchar(30) NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'OnaylayanPersonelId') IS NULL ALTER TABLE dbo.BakimPlanlari ADD OnaylayanPersonelId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'OnayTarihi') IS NULL ALTER TABLE dbo.BakimPlanlari ADD OnayTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.BakimPlanlari ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_BakimPlanlari_Olusturma DEFAULT SYSUTCDATETIME();
    IF COL_LENGTH('dbo.BakimPlanlari', 'GuncellemeTarihi') IS NULL ALTER TABLE dbo.BakimPlanlari ADD GuncellemeTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.BakimPlanlari', 'RowVersion') IS NULL ALTER TABLE dbo.BakimPlanlari ADD RowVersion rowversion;
    ALTER TABLE dbo.BakimPlanlari ALTER COLUMN TahminiMaliyetTl decimal(18,2) NULL;

    IF COL_LENGTH('dbo.BakimKayitlari', 'PlanId') IS NULL ALTER TABLE dbo.BakimKayitlari ADD PlanId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'BakimTuruId') IS NULL ALTER TABLE dbo.BakimKayitlari ADD BakimTuruId int NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'BaslangicTarihi') IS NULL ALTER TABLE dbo.BakimKayitlari ADD BaslangicTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'BitisTarihi') IS NULL ALTER TABLE dbo.BakimKayitlari ADD BitisTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'KaynakTuru') IS NULL ALTER TABLE dbo.BakimKayitlari ADD KaynakTuru nvarchar(30) NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'MakineSonrasiDurum') IS NULL ALTER TABLE dbo.BakimKayitlari ADD MakineSonrasiDurum nvarchar(50) NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'KontrolListesiId') IS NULL ALTER TABLE dbo.BakimKayitlari ADD KontrolListesiId int NULL;
    IF COL_LENGTH('dbo.BakimKayitlari', 'OlusturmaTarihi') IS NULL ALTER TABLE dbo.BakimKayitlari ADD OlusturmaTarihi datetime2(0) NOT NULL CONSTRAINT DF_BakimKayitlari_Olusturma DEFAULT SYSUTCDATETIME();
    ALTER TABLE dbo.BakimKayitlari ALTER COLUMN SonrakiBakimTarihi datetime2(7) NULL;
    ALTER TABLE dbo.BakimKayitlari ALTER COLUMN MaliyetTl decimal(18,2) NULL;

    /* Risk analizi geçmişi, güncel risk ve uyarı izlenebilirliği. */
    IF COL_LENGTH('dbo.RiskAnalizleri', 'HibritSetId') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD HibritSetId int NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'AiRiskSkoru') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD AiRiskSkoru decimal(5,2) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'VeriTamlikOrani') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD VeriTamlikOrani decimal(5,2) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'GuvenSeviyesi') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD GuvenSeviyesi decimal(5,2) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'GirdiSnapshotJson') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD GirdiSnapshotJson nvarchar(max) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'UyarilarJson') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD UyarilarJson nvarchar(max) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'HesaplamaNedeni') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD HesaplamaNedeni nvarchar(200) NULL;
    IF COL_LENGTH('dbo.RiskAnalizleri', 'VeriKaynagi') IS NULL ALTER TABLE dbo.RiskAnalizleri ADD VeriKaynagi nvarchar(50) NOT NULL CONSTRAINT DF_RiskAnalizleri_Kaynak DEFAULT('LOCAL');
    ALTER TABLE dbo.RiskAnalizleri ALTER COLUMN MlOlasiligi decimal(6,4) NULL;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.GuncelRiskler') AND name='UQ_GuncelRiskler_MakineId')
        CREATE UNIQUE INDEX UQ_GuncelRiskler_MakineId ON dbo.GuncelRiskler(MakineId);
    ALTER TABLE dbo.GuncelRiskler ALTER COLUMN RiskPuani decimal(5,2) NOT NULL;
    ALTER TABLE dbo.GuncelRiskler ALTER COLUMN KuralPuani decimal(5,2) NULL;
    ALTER TABLE dbo.GuncelRiskler ALTER COLUMN IstatistikPuani decimal(5,2) NULL;

    IF COL_LENGTH('dbo.Uyarilar', 'AnalizId') IS NULL ALTER TABLE dbo.Uyarilar ADD AnalizId int NULL;
    IF COL_LENGTH('dbo.Uyarilar', 'PlanId') IS NULL ALTER TABLE dbo.Uyarilar ADD PlanId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.Uyarilar', 'KapatmaTarihi') IS NULL ALTER TABLE dbo.Uyarilar ADD KapatmaTarihi datetime2(0) NULL;
    IF COL_LENGTH('dbo.Uyarilar', 'KapatmaNedeni') IS NULL ALTER TABLE dbo.Uyarilar ADD KapatmaNedeni nvarchar(1000) NULL;
    IF COL_LENGTH('dbo.Uyarilar', 'IslemYapanPersonelId') IS NULL ALTER TABLE dbo.Uyarilar ADD IslemYapanPersonelId nvarchar(20) NULL;
    IF COL_LENGTH('dbo.Uyarilar', 'IslemSonucu') IS NULL ALTER TABLE dbo.Uyarilar ADD IslemSonucu nvarchar(100) NULL;

    /* Durum ve işlem geçmişleri. */
    IF OBJECT_ID('dbo.IsEmriDurumGecmisi', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.IsEmriDurumGecmisi(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_IsEmriDurumGecmisi PRIMARY KEY,
            IsEmriId int NOT NULL, EskiDurum nvarchar(50) NULL, YeniDurum nvarchar(50) NOT NULL,
            Tarih datetime2(0) NOT NULL CONSTRAINT DF_IsEmriDurumGecmisi_Tarih DEFAULT SYSUTCDATETIME(),
            PersonelId nvarchar(20) NULL, Aciklama nvarchar(1000) NULL,
            CONSTRAINT FK_IsEmriDurumGecmisi_IsEmri FOREIGN KEY(IsEmriId) REFERENCES dbo.IsEmirleri(IsEmriId),
            CONSTRAINT FK_IsEmriDurumGecmisi_Personel FOREIGN KEY(PersonelId) REFERENCES dbo.Personeller(PersonelId)
        );
        CREATE INDEX IX_IsEmriDurumGecmisi_IsEmriTarih ON dbo.IsEmriDurumGecmisi(IsEmriId,Tarih DESC);
    END;

    IF OBJECT_ID('dbo.UyariIslemGecmisi', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.UyariIslemGecmisi(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_UyariIslemGecmisi PRIMARY KEY,
            UyariId nvarchar(20) NOT NULL, IslemTuru nvarchar(50) NOT NULL,
            EskiDurum nvarchar(30) NULL, YeniDurum nvarchar(30) NULL,
            PersonelId nvarchar(20) NULL,
            IslemTarihi datetime2(0) NOT NULL CONSTRAINT DF_UyariIslemGecmisi_Tarih DEFAULT SYSUTCDATETIME(),
            Aciklama nvarchar(1000) NULL,
            CONSTRAINT FK_UyariIslemGecmisi_Uyari FOREIGN KEY(UyariId) REFERENCES dbo.Uyarilar(UyariId),
            CONSTRAINT FK_UyariIslemGecmisi_Personel FOREIGN KEY(PersonelId) REFERENCES dbo.Personeller(PersonelId)
        );
        CREATE INDEX IX_UyariIslemGecmisi_UyariTarih ON dbo.UyariIslemGecmisi(UyariId,IslemTarihi DESC);
    END;

    IF OBJECT_ID('dbo.BakimPlanDurumGecmisi', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BakimPlanDurumGecmisi(
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_BakimPlanDurumGecmisi PRIMARY KEY,
            PlanId nvarchar(20) NOT NULL, EskiDurum nvarchar(30) NULL, YeniDurum nvarchar(30) NOT NULL,
            PersonelId nvarchar(20) NULL,
            DegisiklikTarihi datetime2(0) NOT NULL CONSTRAINT DF_BakimPlanDurumGecmisi_Tarih DEFAULT SYSUTCDATETIME(),
            Aciklama nvarchar(1000) NULL,
            CONSTRAINT FK_BakimPlanDurumGecmisi_Plan FOREIGN KEY(PlanId) REFERENCES dbo.BakimPlanlari(PlanId),
            CONSTRAINT FK_BakimPlanDurumGecmisi_Personel FOREIGN KEY(PersonelId) REFERENCES dbo.Personeller(PersonelId)
        );
        CREATE INDEX IX_BakimPlanDurumGecmisi_PlanTarih ON dbo.BakimPlanDurumGecmisi(PlanId,DegisiklikTarihi DESC);
    END;

    /* Yeni nullable ilişkiler: eski eşleşmeyen kayıtları silmeden eklenir. */
    IF OBJECT_ID('FK_ArizaKartlari_ArizaKodu', 'F') IS NULL EXEC(N'ALTER TABLE dbo.ArizaKartlari WITH CHECK ADD CONSTRAINT FK_ArizaKartlari_ArizaKodu FOREIGN KEY(ArizaKoduId) REFERENCES dbo.ArizaKoduTanimlari(ArizaKoduId);');
    IF OBJECT_ID('FK_ArizaKartlari_Vardiya', 'F') IS NULL EXEC(N'ALTER TABLE dbo.ArizaKartlari WITH CHECK ADD CONSTRAINT FK_ArizaKartlari_Vardiya FOREIGN KEY(VardiyaId) REFERENCES dbo.Vardiyalar(VardiyaId);');
    IF OBJECT_ID('FK_ArizaKartlari_Bildiren', 'F') IS NULL EXEC(N'ALTER TABLE dbo.ArizaKartlari WITH CHECK ADD CONSTRAINT FK_ArizaKartlari_Bildiren FOREIGN KEY(BildirenPersonelId) REFERENCES dbo.Personeller(PersonelId);');
    IF OBJECT_ID('FK_ArizaKartlari_KapanisPersonel', 'F') IS NULL EXEC(N'ALTER TABLE dbo.ArizaKartlari WITH CHECK ADD CONSTRAINT FK_ArizaKartlari_KapanisPersonel FOREIGN KEY(KapanisPersonelId) REFERENCES dbo.Personeller(PersonelId);');
    IF OBJECT_ID('FK_BakimPlanlari_Analiz', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimPlanlari WITH CHECK ADD CONSTRAINT FK_BakimPlanlari_Analiz FOREIGN KEY(AnalizId) REFERENCES dbo.RiskAnalizleri(AnalizId);');
    IF OBJECT_ID('FK_BakimPlanlari_Uyari', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimPlanlari WITH CHECK ADD CONSTRAINT FK_BakimPlanlari_Uyari FOREIGN KEY(UyariId) REFERENCES dbo.Uyarilar(UyariId);');
    IF OBJECT_ID('FK_BakimPlanlari_BakimTuru', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimPlanlari WITH CHECK ADD CONSTRAINT FK_BakimPlanlari_BakimTuru FOREIGN KEY(BakimTuruId) REFERENCES dbo.BakimTuruTanimlari(BakimTuruId);');
    IF OBJECT_ID('FK_BakimPlanlari_Ekip', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimPlanlari WITH CHECK ADD CONSTRAINT FK_BakimPlanlari_Ekip FOREIGN KEY(SorumluEkipId) REFERENCES dbo.Ekipler(EkipId);');
    IF OBJECT_ID('FK_BakimPlanlari_Onaylayan', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimPlanlari WITH CHECK ADD CONSTRAINT FK_BakimPlanlari_Onaylayan FOREIGN KEY(OnaylayanPersonelId) REFERENCES dbo.Personeller(PersonelId);');
    IF OBJECT_ID('FK_BakimKayitlari_Plan', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimKayitlari WITH CHECK ADD CONSTRAINT FK_BakimKayitlari_Plan FOREIGN KEY(PlanId) REFERENCES dbo.BakimPlanlari(PlanId);');
    IF OBJECT_ID('FK_BakimKayitlari_BakimTuru', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimKayitlari WITH CHECK ADD CONSTRAINT FK_BakimKayitlari_BakimTuru FOREIGN KEY(BakimTuruId) REFERENCES dbo.BakimTuruTanimlari(BakimTuruId);');
    IF OBJECT_ID('FK_BakimKayitlari_KontrolListesi', 'F') IS NULL EXEC(N'ALTER TABLE dbo.BakimKayitlari WITH CHECK ADD CONSTRAINT FK_BakimKayitlari_KontrolListesi FOREIGN KEY(KontrolListesiId) REFERENCES dbo.BakimKontrolListeleri(KontrolListesiId);');
    IF OBJECT_ID('FK_RiskAnalizleri_HibritSet', 'F') IS NULL EXEC(N'ALTER TABLE dbo.RiskAnalizleri WITH CHECK ADD CONSTRAINT FK_RiskAnalizleri_HibritSet FOREIGN KEY(HibritSetId) REFERENCES dbo.HibritKararParametreleri(HibritSetId);');
    IF OBJECT_ID('FK_Uyarilar_Analiz', 'F') IS NULL EXEC(N'ALTER TABLE dbo.Uyarilar WITH CHECK ADD CONSTRAINT FK_Uyarilar_Analiz FOREIGN KEY(AnalizId) REFERENCES dbo.RiskAnalizleri(AnalizId);');
    IF OBJECT_ID('FK_Uyarilar_Plan', 'F') IS NULL EXEC(N'ALTER TABLE dbo.Uyarilar WITH CHECK ADD CONSTRAINT FK_Uyarilar_Plan FOREIGN KEY(PlanId) REFERENCES dbo.BakimPlanlari(PlanId);');
    IF OBJECT_ID('FK_Uyarilar_IslemYapan', 'F') IS NULL EXEC(N'ALTER TABLE dbo.Uyarilar WITH CHECK ADD CONSTRAINT FK_Uyarilar_IslemYapan FOREIGN KEY(IslemYapanPersonelId) REFERENCES dbo.Personeller(PersonelId);');

    /* Aynı makine için tek aktif hibrit plan. */
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.BakimPlanlari') AND name='UQ_BakimPlanlari_AktifHibrit')
        EXEC(N'CREATE UNIQUE INDEX UQ_BakimPlanlari_AktifHibrit ON dbo.BakimPlanlari(MakineId)
        WHERE AnalizId IS NOT NULL AND Durum IN (N''Taslak'',N''Planlandı'',N''Onaylandı'',N''Devam Ediyor'');');

    /* İdempotent development başlangıç verileri. */
    IF NOT EXISTS(SELECT 1 FROM dbo.Vardiyalar WHERE VardiyaKodu='V1')
        INSERT dbo.Vardiyalar(VardiyaKodu,VardiyaAdi,BaslangicSaati,BitisSaati,Aktif,Aciklama) VALUES('V1',N'Sabah','06:00','14:00',1,N'Yerel başlangıç vardiyası');
    IF NOT EXISTS(SELECT 1 FROM dbo.Vardiyalar WHERE VardiyaKodu='V2')
        INSERT dbo.Vardiyalar(VardiyaKodu,VardiyaAdi,BaslangicSaati,BitisSaati,Aktif,Aciklama) VALUES('V2',N'Akşam','14:00','22:00',1,N'Yerel başlangıç vardiyası');
    IF NOT EXISTS(SELECT 1 FROM dbo.Vardiyalar WHERE VardiyaKodu='V3')
        INSERT dbo.Vardiyalar(VardiyaKodu,VardiyaAdi,BaslangicSaati,BitisSaati,Aktif,Aciklama) VALUES('V3',N'Gece','22:00','06:00',1,N'Yerel başlangıç vardiyası');

    IF NOT EXISTS(SELECT 1 FROM dbo.MakineSayacTanimlari WHERE SayacKodu='CALISMA-SAATI')
        INSERT dbo.MakineSayacTanimlari(SayacKodu,SayacAdi,SayacTuru,Birim,Aktif) VALUES('CALISMA-SAATI',N'Çalışma Saati',N'Kümülatif',N'Saat',1);
    IF NOT EXISTS(SELECT 1 FROM dbo.MakineSayacTanimlari WHERE SayacKodu='CEVRIM')
        INSERT dbo.MakineSayacTanimlari(SayacKodu,SayacAdi,SayacTuru,Birim,Aktif) VALUES('CEVRIM',N'Çevrim Sayacı',N'Kümülatif',N'Adet',1);

    DELETE FROM dbo.BakimOneriParametreleri WHERE OneriKodu IN ('ONR-DUSUK','ONR-ORTA','ONR-YUKSEK','ONR-KRITIK');
    EXEC(N'INSERT dbo.BakimOneriParametreleri
        (OneriKodu,RiskSeviyesi,MinPuan,MaxPuan,MaxDahil,OnerilenGun,VarsayilanBakimTuru,VarsayilanOncelik,OtomatikUyariOlustur,OtomatikPlanTaslagi,YoneticiOnayiGerekli,GerekceZorunlu,BildirimGoster,IslemGerekmiyorSecenegi,VarsayilanKarar,SorumluRol,Aktif,Aciklama)
    VALUES
        (''ONR-DUSUK'', N''Düşük'',   0,  30,0,60,N''Periyodik'',N''Düşük'', 0,0,0,0,1,1,N''İzle'',N''Bakım Yöneticisi'',1,N''0 <= skor < 30''),
        (''ONR-ORTA'',  N''Orta'',   30,  60,0,21,N''Planlı'',  N''Orta'',  1,1,0,0,1,1,N''Planla'',N''Bakım Yöneticisi'',1,N''30 <= skor < 60''),
        (''ONR-YUKSEK'',N''Yüksek'', 60,  80,0, 7,N''Acil'',    N''Yüksek'',1,1,1,1,1,0,N''Planla'',N''Bakım Yöneticisi'',1,N''60 <= skor < 80''),
        (''ONR-KRITIK'',N''Kritik'', 80, 100,1, 3,N''Acil müdahale'',N''Kritik'',1,1,1,1,1,0,N''Acil Planla'',N''Bakım Yöneticisi'',1,N''80 <= skor <= 100'');');

    IF NOT EXISTS(SELECT 1 FROM dbo.HibritKararParametreleri WHERE SetKodu='HIBRIT-V1')
        EXEC(N'INSERT dbo.HibritKararParametreleri(SetKodu,SetAdi,ModelAdi,ModelVersiyonu,KuralSetiId,MlAgirligi,KuralAgirligi,MinGecmisKayit,ModelGuvenEsigi,EksikVeriYontemi,VeriYeterlilikOrani,UyumsuzlukEsigi,BuyukFarktaEngelle,ManuelIncelemeGerekli,OtomatikPlanTaslagi,Aktif,Aciklama)
        SELECT ''HIBRIT-V1'',N''Veri Tamlığına Göre Hibrit Karar'',N''predictive-maintenance'',N''1.0'',MIN(KuralSetiId),40,60,1,50,N''Kural tabanlı devam'',50,30,0,1,1,1,N''Ağırlıklar analiz anındaki veri tamlığına göre dinamik seçilir.'' FROM dbo.RiskKuralSetleri;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Betik doğrulama özeti. */
SELECT t.name AS Tablo, SUM(p.rows) AS KayitSayisi
FROM sys.tables t
JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN(0,1)
WHERE t.name IN ('MakineSayacTanimlari','Vardiyalar','MakineSayacKayitlari','MakineCalismaTakvimleri',
                 'MakineTransferleri','ArizaDegisenParcalar','Ekipler','EkipUyeleri',
                 'IsEmriDurumGecmisi','UyariIslemGecmisi','BakimPlanDurumGecmisi')
GROUP BY t.name ORDER BY t.name;
GO
