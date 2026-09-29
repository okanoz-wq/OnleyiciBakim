using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "AnalizTarihi", Name = "IX_RiskAnalizleri_Makine", IsDescending = new[] { false, true })]
[Index("AnalizNo", Name = "UQ_RiskAnalizleri", IsUnique = true)]
public partial class RiskAnalizleri
{
    [Key]
    public int AnalizId { get; set; }

    [StringLength(25)]
    public string AnalizNo { get; set; } = null!;

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    public DateTime AnalizTarihi { get; set; }

    public int? KuralSetiId { get; set; }

    public int? HibritSetId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? KuralPuani { get; set; }

    [Column(TypeName = "decimal(6, 4)")]
    public decimal? MlOlasiligi { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? AiRiskSkoru { get; set; }

    [StringLength(20)]
    public string? ModelVersiyonu { get; set; }

    public int? MlAgirligi { get; set; }

    public int? KuralAgirligi { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal NihaiPuan { get; set; }

    [StringLength(20)]
    public string? RiskSeviyesi { get; set; }

    [StringLength(20)]
    public string? VeriYeterliligi { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? VeriTamlikOrani { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? GuvenSeviyesi { get; set; }

    public string? GirdiSnapshotJson { get; set; }

    public string? UyarilarJson { get; set; }

    [StringLength(200)] public string? HesaplamaNedeni { get; set; }

    [StringLength(50)] public string VeriKaynagi { get; set; } = "LOCAL";

    public DateOnly? OnerilenBakimTarihi { get; set; }

    [StringLength(50)]
    public string? Durum { get; set; }

    [InverseProperty("Analiz")]
    public virtual ICollection<KararGecmisi> KararGecmisi { get; set; } = new List<KararGecmisi>();

    [ForeignKey("KuralSetiId")]
    [InverseProperty("RiskAnalizleri")]
    public virtual RiskKuralSetleri? KuralSeti { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("RiskAnalizleri")]
    public virtual Makineler Makine { get; set; } = null!;

    [InverseProperty("Analiz")]
    public virtual ICollection<RiskAnalizFaktorleri> RiskAnalizFaktorleri { get; set; } = new List<RiskAnalizFaktorleri>();
}
