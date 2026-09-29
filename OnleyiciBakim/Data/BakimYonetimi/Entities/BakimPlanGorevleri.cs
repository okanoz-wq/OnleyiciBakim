using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("PlanId", "SiraNo", Name = "UQ_PlanGorevleri", IsUnique = true)]
public partial class BakimPlanGorevleri
{
    [Key]
    public int GorevId { get; set; }

    [StringLength(20)]
    public string PlanId { get; set; } = null!;

    public int SiraNo { get; set; }

    [StringLength(300)]
    public string Gorev { get; set; } = null!;

    [StringLength(20)]
    public string? SorumluPersonelId { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? TahminiSureSaat { get; set; }

    [StringLength(50)]
    public string? Durum { get; set; }

    [ForeignKey("PlanId")]
    [InverseProperty("BakimPlanGorevleri")]
    public virtual BakimPlanlari Plan { get; set; } = null!;

    [ForeignKey("SorumluPersonelId")]
    [InverseProperty("BakimPlanGorevleri")]
    public virtual Personeller? SorumluPersonel { get; set; }
}
