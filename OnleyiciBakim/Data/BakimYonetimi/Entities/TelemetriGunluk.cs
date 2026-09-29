using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "Tarih", Name = "IX_TelemetriGunluk_MakineId_Tarih", IsUnique = true)]
public partial class TelemetriGunluk
{
    [Key]
    public int Id { get; set; }

    public DateTime Tarih { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [Column(TypeName = "decimal(10, 4)")]
    public decimal VoltOrt { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal VoltStd { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal DevirOrt { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal DevirStd { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal BasincOrt { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal BasincStd { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal TitresimOrt { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal TitresimStd { get; set; }

    public int KayitSayisi { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal AnomaliPuani { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal UretimYogunluguYuzde { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("TelemetriGunluk")]
    public virtual Makineler Makine { get; set; } = null!;
}
