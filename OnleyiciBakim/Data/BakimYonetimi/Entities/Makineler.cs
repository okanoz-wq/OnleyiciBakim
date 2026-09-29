using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("DepartmanId", Name = "IX_Makineler_DepartmanId")]
[Index("FirmaId", Name = "IX_Makineler_FirmaId")]
[Index("HatId", Name = "IX_Makineler_HatId")]
[Index("SubeId", Name = "IX_Makineler_SubeId")]
public partial class Makineler
{
    [Key]
    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    public int? KaynakMakineId { get; set; }

    [StringLength(20)]
    public string FirmaId { get; set; } = null!;

    [StringLength(20)]
    public string SubeId { get; set; } = null!;

    [StringLength(20)]
    public string DepartmanId { get; set; } = null!;

    [StringLength(20)]
    public string HatId { get; set; } = null!;

    [StringLength(50)]
    public string MakineKodu { get; set; } = null!;

    [StringLength(200)]
    public string MakineAdi { get; set; } = null!;

    [StringLength(100)]
    public string? MakineTuru { get; set; }

    [StringLength(100)]
    public string? Marka { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? SeriNo { get; set; }

    public int Yas { get; set; }

    [StringLength(20)]
    public string? KritikSeviye { get; set; }

    public int KurulumYili { get; set; }

    public DateOnly? KurulumTarihi { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    public DateTime? SonBakimTarihi { get; set; }

    public DateTime? SonArizaTarihi { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? GuncelRiskPuani { get; set; }

    [StringLength(20)]
    public string? GuncelRiskSeviyesi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(50)]
    public string? PeriyotTuru { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal? PeriyotDegeri { get; set; }

    [StringLength(30)]
    public string? PeriyotBirimi { get; set; }

    public bool Aktif { get; set; } = true;

    [StringLength(1000)]
    public string? Aciklama { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [InverseProperty("Makine")]
    public virtual ICollection<ArizaKartlari> ArizaKartlari { get; set; } = new List<ArizaKartlari>();

    [InverseProperty("Makine")]
    public virtual ICollection<BakimKayitlari> BakimKayitlari { get; set; } = new List<BakimKayitlari>();

    [InverseProperty("Makine")]
    public virtual ICollection<BakimPlanlari> BakimPlanlari { get; set; } = new List<BakimPlanlari>();

    [ForeignKey("DepartmanId")]
    [InverseProperty("Makineler")]
    public virtual Departmanlar Departman { get; set; } = null!;

    [ForeignKey("FirmaId")]
    [InverseProperty("Makineler")]
    public virtual Firmalar Firma { get; set; } = null!;

    [InverseProperty("Makine")]
    public virtual ICollection<GuncelRiskler> GuncelRiskler { get; set; } = new List<GuncelRiskler>();

    [ForeignKey("HatId")]
    [InverseProperty("Makineler")]
    public virtual UretimHatlari Hat { get; set; } = null!;

    [InverseProperty("Makine")]
    public virtual ICollection<HataKayitlari> HataKayitlari { get; set; } = new List<HataKayitlari>();

    [InverseProperty("Makine")]
    public virtual ICollection<IsEmirleri> IsEmirleri { get; set; } = new List<IsEmirleri>();

    [InverseProperty("Makine")]
    public virtual ICollection<KararGecmisi> KararGecmisi { get; set; } = new List<KararGecmisi>();

    [InverseProperty("Makine")]
    public virtual ICollection<MakineBilesenler> MakineBilesenler { get; set; } = new List<MakineBilesenler>();

    [InverseProperty("Makine")]
    public virtual ICollection<RiskAnalizleri> RiskAnalizleri { get; set; } = new List<RiskAnalizleri>();

    [ForeignKey("SubeId")]
    [InverseProperty("Makineler")]
    public virtual Subeler Sube { get; set; } = null!;

    [InverseProperty("Makine")]
    public virtual ICollection<TelemetriGunluk> TelemetriGunluk { get; set; } = new List<TelemetriGunluk>();

    [InverseProperty("Makine")]
    public virtual ICollection<Uyarilar> Uyarilar { get; set; } = new List<Uyarilar>();
}
