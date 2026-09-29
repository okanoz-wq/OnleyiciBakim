using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("BagliArizaId", Name = "IX_BakimKayitlari_BagliArizaId")]
[Index("MakineId", "BakimTarihi", Name = "IX_BakimKayitlari_MakineId_BakimTarihi")]
[Index("PersonelId", Name = "IX_BakimKayitlari_PersonelId")]
public partial class BakimKayitlari
{
    [Key]
    [StringLength(20)]
    public string BakimId { get; set; } = null!;

    public DateTime BakimTarihi { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [StringLength(20)] public string? PlanId { get; set; }

    public int? BakimTuruId { get; set; }

    [StringLength(20)]
    public string? BilesenKodu { get; set; }

    [StringLength(100)]
    public string? BilesenAdi { get; set; }

    [StringLength(30)]
    public string? BakimTuru { get; set; }

    [StringLength(200)]
    public string? BakimNedeni { get; set; }

    public int SureDk { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    [StringLength(200)]
    public string? Sonuc { get; set; }

    [StringLength(20)]
    public string? PersonelId { get; set; }

    public DateTime? SonrakiBakimTarihi { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaliyetTl { get; set; }

    [StringLength(20)]
    public string? BagliArizaId { get; set; }

    [StringLength(50)]
    public string? IsEmriNo { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    [StringLength(30)] public string? KaynakTuru { get; set; }
    [StringLength(50)] public string? MakineSonrasiDurum { get; set; }
    public int? KontrolListesiId { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    [ForeignKey("BagliArizaId")]
    [InverseProperty("BakimKayitlari")]
    public virtual ArizaKartlari? BagliAriza { get; set; }

    [InverseProperty("Bakim")]
    public virtual ICollection<BakimDegisenParcalar> BakimDegisenParcalar { get; set; } = new List<BakimDegisenParcalar>();

    [InverseProperty("Bakim")]
    public virtual ICollection<BakimKontrolSonuclari> BakimKontrolSonuclari { get; set; } = new List<BakimKontrolSonuclari>();

    [ForeignKey("MakineId")]
    [InverseProperty("BakimKayitlari")]
    public virtual Makineler Makine { get; set; } = null!;

    [ForeignKey("PersonelId")]
    [InverseProperty("BakimKayitlari")]
    public virtual Personeller? Personel { get; set; }
}
