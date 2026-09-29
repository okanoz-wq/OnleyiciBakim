using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "ArizaTarihi", Name = "IX_ArizaKartlari_MakineId_ArizaTarihi")]
[Index("PersonelId", Name = "IX_ArizaKartlari_PersonelId")]
public partial class ArizaKartlari
{
    [Key]
    [StringLength(20)]
    public string ArizaId { get; set; } = null!;

    public DateTime ArizaTarihi { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    public int? ArizaKoduId { get; set; }

    public int? VardiyaId { get; set; }

    [StringLength(20)]
    public string? BilesenKodu { get; set; }

    [StringLength(100)]
    public string? BilesenAdi { get; set; }

    [StringLength(150)]
    public string? ArizaTuru { get; set; }

    [StringLength(20)]
    public string? OnemSeviyesi { get; set; }

    public bool TekrarEdenMi { get; set; }

    public bool PlanliDurusMu { get; set; }

    public int? IlkMudahaleDk { get; set; }

    public int? MudahaleSuresiDk { get; set; }

    public int? DurusSuresiDk { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    [StringLength(200)]
    public string? KokNeden { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [StringLength(20)]
    public string? PersonelId { get; set; }

    [StringLength(50)]
    public string? IsEmriNo { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TahminiMaliyetTl { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(100)] public string? UrunStokKodu { get; set; }
    [StringLength(1000)] public string? IlkBelirti { get; set; }
    public bool UretimDurduMu { get; set; }
    public int? TahminiDurusSuresiDk { get; set; }
    [StringLength(20)] public string? BildirenPersonelId { get; set; }
    public DateTime? ArizaBitisTarihi { get; set; }
    [Column(TypeName = "decimal(18,3)")] public decimal? UretimKaybiMiktari { get; set; }
    [StringLength(30)] public string? UretimKaybiBirimi { get; set; }
    public bool Aktif { get; set; } = true;
    [StringLength(20)] public string? KapanisPersonelId { get; set; }
    public DateTime? KapanisTarihi { get; set; }
    [StringLength(2000)] public string? CozumAciklamasi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    [InverseProperty("Ariza")]
    public virtual ICollection<ArizaMudahaleler> ArizaMudahaleler { get; set; } = new List<ArizaMudahaleler>();

    [InverseProperty("BagliAriza")]
    public virtual ICollection<BakimKayitlari> BakimKayitlari { get; set; } = new List<BakimKayitlari>();

    [InverseProperty("BagliAriza")]
    public virtual ICollection<HataKayitlari> HataKayitlari { get; set; } = new List<HataKayitlari>();

    [InverseProperty("Ariza")]
    public virtual ICollection<IsEmirleri> IsEmirleri { get; set; } = new List<IsEmirleri>();

    [ForeignKey("MakineId")]
    [InverseProperty("ArizaKartlari")]
    public virtual Makineler Makine { get; set; } = null!;

    [ForeignKey("PersonelId")]
    [InverseProperty("ArizaKartlari")]
    public virtual Personeller? Personel { get; set; }
}
