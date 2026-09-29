using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Table("MakineSayacTanimlari")]
[Index(nameof(SayacKodu), IsUnique = true, Name = "UQ_MakineSayacTanimlari_SayacKodu")]
public sealed class MakineSayacTanimlari
{
    [Key] public int SayacTanimId { get; set; }
    [StringLength(30)] public string SayacKodu { get; set; } = null!;
    [StringLength(150)] public string SayacAdi { get; set; } = null!;
    [StringLength(50)] public string SayacTuru { get; set; } = null!;
    [StringLength(30)] public string Birim { get; set; } = null!;
    public bool Aktif { get; set; } = true;
    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

[Table("Vardiyalar")]
[Index(nameof(VardiyaKodu), IsUnique = true, Name = "UQ_Vardiyalar_VardiyaKodu")]
public sealed class Vardiyalar
{
    [Key] public int VardiyaId { get; set; }
    [StringLength(20)] public string VardiyaKodu { get; set; } = null!;
    [StringLength(100)] public string VardiyaAdi { get; set; } = null!;
    public TimeOnly BaslangicSaati { get; set; }
    public TimeOnly BitisSaati { get; set; }
    public bool Aktif { get; set; } = true;
    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

[Table("MakineSayacKayitlari")]
[Index(nameof(MakineId), nameof(SayacTanimId), nameof(Tarih), nameof(VardiyaId), IsUnique = true, Name = "UQ_MakineSayacKayitlari")]
public sealed class MakineSayacKayitlari
{
    [Key] public long SayacKayitId { get; set; }
    [StringLength(20)] public string MakineId { get; set; } = null!;
    public int SayacTanimId { get; set; }
    public DateOnly Tarih { get; set; }
    public int VardiyaId { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal BaslangicSayac { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal BitisSayac { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal SayacFarki { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? CevrimSayisi { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? UretimMiktari { get; set; }
    public int? CalismaSuresiDk { get; set; }
    public int? BostaKalmaSuresiDk { get; set; }
    public int? UretimSuresiDk { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? VeriKalitesi { get; set; }
    [StringLength(50)] public string VeriKaynagi { get; set; } = "LOCAL";
    [StringLength(1000)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

[Table("MakineCalismaTakvimleri")]
[Index(nameof(MakineId), nameof(Tarih), nameof(VardiyaId), IsUnique = true, Name = "UQ_MakineCalismaTakvimleri")]
public sealed class MakineCalismaTakvimleri
{
    [Key] public long Id { get; set; }
    [StringLength(20)] public string MakineId { get; set; } = null!;
    public DateOnly Tarih { get; set; }
    public int VardiyaId { get; set; }
    public int PlanlananCalismaSuresiDk { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? PlanlananUretimMiktari { get; set; }
    [StringLength(30)] public string Durum { get; set; } = "Planlandı";
    [StringLength(1000)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

[Table("MakineTransferleri")]
public sealed class MakineTransferleri
{
    [Key] public long TransferId { get; set; }
    [StringLength(20)] public string MakineId { get; set; } = null!;
    [StringLength(20)] public string? EskiFirmaId { get; set; }
    [StringLength(20)] public string? EskiSubeId { get; set; }
    [StringLength(20)] public string? EskiDepartmanId { get; set; }
    [StringLength(20)] public string? EskiHatId { get; set; }
    [StringLength(20)] public string YeniFirmaId { get; set; } = null!;
    [StringLength(20)] public string YeniSubeId { get; set; } = null!;
    [StringLength(20)] public string YeniDepartmanId { get; set; } = null!;
    [StringLength(20)] public string YeniHatId { get; set; } = null!;
    public DateTime TransferTarihi { get; set; }
    [StringLength(1000)] public string Gerekce { get; set; } = null!;
    [StringLength(20)] public string? IslemiYapanPersonelId { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

[Table("Ekipler")]
[Index(nameof(EkipKodu), IsUnique = true, Name = "UQ_Ekipler_EkipKodu")]
public sealed class Ekipler
{
    [Key] public int EkipId { get; set; }
    [StringLength(30)] public string EkipKodu { get; set; } = null!;
    [StringLength(150)] public string EkipAdi { get; set; } = null!;
    public bool Aktif { get; set; } = true;
    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

[Table("EkipUyeleri")]
[Index(nameof(EkipId), nameof(PersonelId), nameof(BaslangicTarihi), IsUnique = true, Name = "UQ_EkipUyeleri")]
public sealed class EkipUyeleri
{
    [Key] public long Id { get; set; }
    public int EkipId { get; set; }
    [StringLength(20)] public string PersonelId { get; set; } = null!;
    public bool Aktif { get; set; } = true;
    public DateOnly BaslangicTarihi { get; set; }
    public DateOnly? BitisTarihi { get; set; }
}

[Table("ArizaDegisenParcalar")]
public sealed class ArizaDegisenParcalar
{
    [Key] public long Id { get; set; }
    public int MudahaleId { get; set; }
    public int? ParcaId { get; set; }
    [StringLength(20)] public string? ParcaKodu { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal Miktar { get; set; }
    [StringLength(500)] public string? Aciklama { get; set; }
}

[Table("IsEmriDurumGecmisi")]
public sealed class IsEmriDurumGecmisi
{
    [Key] public long Id { get; set; }
    public int IsEmriId { get; set; }
    [StringLength(50)] public string? EskiDurum { get; set; }
    [StringLength(50)] public string YeniDurum { get; set; } = null!;
    public DateTime Tarih { get; set; }
    [StringLength(20)] public string? PersonelId { get; set; }
    [StringLength(1000)] public string? Aciklama { get; set; }
}

[Table("UyariIslemGecmisi")]
public sealed class UyariIslemGecmisi
{
    [Key] public long Id { get; set; }
    [StringLength(20)] public string UyariId { get; set; } = null!;
    [StringLength(50)] public string IslemTuru { get; set; } = null!;
    [StringLength(30)] public string? EskiDurum { get; set; }
    [StringLength(30)] public string? YeniDurum { get; set; }
    [StringLength(20)] public string? PersonelId { get; set; }
    public DateTime IslemTarihi { get; set; }
    [StringLength(1000)] public string? Aciklama { get; set; }
}

[Table("BakimPlanDurumGecmisi")]
public sealed class BakimPlanDurumGecmisi
{
    [Key] public long Id { get; set; }
    [StringLength(20)] public string PlanId { get; set; } = null!;
    [StringLength(30)] public string? EskiDurum { get; set; }
    [StringLength(30)] public string YeniDurum { get; set; } = null!;
    [StringLength(20)] public string? PersonelId { get; set; }
    public DateTime DegisiklikTarihi { get; set; }
    [StringLength(1000)] public string? Aciklama { get; set; }
}
