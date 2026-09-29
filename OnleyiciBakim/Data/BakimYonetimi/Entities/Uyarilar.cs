using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("Durum", Name = "IX_Uyarilar_Durum")]
[Index("MakineId", Name = "IX_Uyarilar_MakineId")]
[Index("RiskId", Name = "IX_Uyarilar_RiskId")]
[Index("SorumluPersonelId", Name = "IX_Uyarilar_SorumluPersonelId")]
public partial class Uyarilar
{
    [Key]
    [StringLength(20)]
    public string UyariId { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [StringLength(20)]
    public string? RiskId { get; set; }

    public int? AnalizId { get; set; }

    [StringLength(20)] public string? PlanId { get; set; }

    [StringLength(20)]
    public string? UyariSeviyesi { get; set; }

    [StringLength(300)]
    public string? Baslik { get; set; }

    [StringLength(1000)]
    public string? Aciklama { get; set; }

    [StringLength(500)]
    public string? OnerilenAksiyon { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    public DateTime SonTarih { get; set; }

    [StringLength(20)]
    public string? SorumluPersonelId { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    public DateTime? KapatmaTarihi { get; set; }
    [StringLength(1000)] public string? KapatmaNedeni { get; set; }
    [StringLength(20)] public string? IslemYapanPersonelId { get; set; }
    [StringLength(100)] public string? IslemSonucu { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("Uyarilar")]
    public virtual Makineler Makine { get; set; } = null!;

    [ForeignKey("RiskId")]
    [InverseProperty("Uyarilar")]
    public virtual GuncelRiskler? Risk { get; set; }

    [ForeignKey("SorumluPersonelId")]
    [InverseProperty("Uyarilar")]
    public virtual Personeller? SorumluPersonel { get; set; }

    [InverseProperty("Uyari")]
    public virtual ICollection<UyariNedenleri> UyariNedenleri { get; set; } = new List<UyariNedenleri>();
}
