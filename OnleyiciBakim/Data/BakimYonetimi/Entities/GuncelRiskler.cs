using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "DegerlendirmeTarihi", Name = "IX_GuncelRiskler_MakineId_DegerlendirmeTarihi")]
public partial class GuncelRiskler
{
    [Key]
    [StringLength(20)]
    public string RiskId { get; set; } = null!;

    public DateTime DegerlendirmeTarihi { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RiskPuani { get; set; }

    [StringLength(20)]
    public string? RiskSeviyesi { get; set; }

    public int SonBakimdanGecenGun { get; set; }

    public int ArizaSayisi30g { get; set; }

    public int HataSayisi7g { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal AnomaliPuani7g { get; set; }

    [StringLength(1000)]
    public string? RiskNedenleri { get; set; }

    [StringLength(500)]
    public string? OnerilenAksiyon { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? KuralPuani { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? IstatistikPuani { get; set; }

    [Column(TypeName = "decimal(6, 4)")]
    public decimal? MlOlasiligi { get; set; }

    public bool? ModelDevrede { get; set; }

    [StringLength(50)]
    public string? ModelVersiyonu { get; set; }

    [StringLength(20)]
    public string? VeriYeterliligi { get; set; }

    [StringLength(20)]
    public string? GuvenSeviyesi { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("GuncelRiskler")]
    public virtual Makineler Makine { get; set; } = null!;

    [InverseProperty("Risk")]
    public virtual ICollection<Uyarilar> Uyarilar { get; set; } = new List<Uyarilar>();
}
