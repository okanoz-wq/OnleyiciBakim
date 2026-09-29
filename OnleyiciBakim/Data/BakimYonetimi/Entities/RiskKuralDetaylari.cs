using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("KuralSetiId", "KuralKodu", Name = "UQ_KuralDetaylari", IsUnique = true)]
public partial class RiskKuralDetaylari
{
    [Key]
    public int KuralId { get; set; }

    public int KuralSetiId { get; set; }

    [StringLength(20)]
    public string KuralKodu { get; set; } = null!;

    [StringLength(100)]
    public string RiskFaktoru { get; set; } = null!;

    [StringLength(50)]
    public string? ZamanPenceresi { get; set; }

    [StringLength(10)]
    public string? Operator { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? AltDeger { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? UstDeger { get; set; }

    public int RiskPuani { get; set; }

    public bool HardOverride { get; set; }

    public bool Aktif { get; set; }

    [ForeignKey("KuralSetiId")]
    [InverseProperty("RiskKuralDetaylari")]
    public virtual RiskKuralSetleri KuralSeti { get; set; } = null!;
}
