using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "PlanlananTarih", Name = "IX_IsEmirleri_Makine")]
[Index("IsEmriNo", Name = "UQ_IsEmirleri", IsUnique = true)]
public partial class IsEmirleri
{
    [Key]
    public int IsEmriId { get; set; }

    [StringLength(25)]
    public string IsEmriNo { get; set; } = null!;

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [StringLength(20)]
    public string? PlanId { get; set; }

    [StringLength(20)]
    public string? ArizaId { get; set; }

    [StringLength(50)]
    public string? DevirKaynagi { get; set; }

    [StringLength(20)]
    public string? Oncelik { get; set; }

    public DateOnly? PlanlananTarih { get; set; }

    [StringLength(20)]
    public string? SorumluPersonelId { get; set; }

    [StringLength(50)]
    public string? Durum { get; set; }

    [StringLength(50)]
    public string? ErpSistemi { get; set; }

    [StringLength(50)]
    public string? ErpDurumu { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("ArizaId")]
    [InverseProperty("IsEmirleri")]
    public virtual ArizaKartlari? Ariza { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("IsEmirleri")]
    public virtual Makineler Makine { get; set; } = null!;

    [ForeignKey("PlanId")]
    [InverseProperty("IsEmirleri")]
    public virtual BakimPlanlari? Plan { get; set; }

    [ForeignKey("SorumluPersonelId")]
    [InverseProperty("IsEmirleri")]
    public virtual Personeller? SorumluPersonel { get; set; }
}
