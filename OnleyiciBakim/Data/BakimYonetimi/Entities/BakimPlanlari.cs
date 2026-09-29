using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", Name = "IX_BakimPlanlari_MakineId")]
[Index("PlanlananTarih", Name = "IX_BakimPlanlari_PlanlananTarih")]
[Index("SorumluPersonelId", Name = "IX_BakimPlanlari_SorumluPersonelId")]
public partial class BakimPlanlari
{
    [Key]
    [StringLength(20)]
    public string PlanId { get; set; } = null!;

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    public int? AnalizId { get; set; }

    [StringLength(20)] public string? UyariId { get; set; }

    public DateOnly? OnerilenTarih { get; set; }

    public int? BakimTuruId { get; set; }

    public int? SorumluEkipId { get; set; }

    [StringLength(20)]
    public string? BilesenKodu { get; set; }

    [StringLength(100)]
    public string? BilesenAdi { get; set; }

    public DateTime PlanlananTarih { get; set; }

    [StringLength(30)]
    public string? BakimTuru { get; set; }

    [StringLength(20)]
    public string? Oncelik { get; set; }

    public int TahminiSureDk { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TahminiMaliyetTl { get; set; }

    [StringLength(20)]
    public string? SorumluPersonelId { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    [StringLength(1000)]
    public string? Gerekce { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(30)] public string? OnayDurumu { get; set; }
    [StringLength(20)] public string? OnaylayanPersonelId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    [InverseProperty("Plan")]
    public virtual ICollection<BakimPlanGorevleri> BakimPlanGorevleri { get; set; } = new List<BakimPlanGorevleri>();

    [InverseProperty("Plan")]
    public virtual ICollection<IsEmirleri> IsEmirleri { get; set; } = new List<IsEmirleri>();

    [ForeignKey("MakineId")]
    [InverseProperty("BakimPlanlari")]
    public virtual Makineler Makine { get; set; } = null!;

    [ForeignKey("SorumluPersonelId")]
    [InverseProperty("BakimPlanlari")]
    public virtual Personeller? SorumluPersonel { get; set; }
}
