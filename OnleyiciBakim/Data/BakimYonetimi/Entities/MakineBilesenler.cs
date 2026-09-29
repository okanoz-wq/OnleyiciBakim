using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "BilesenKodu", Name = "UQ_MakineBilesenler", IsUnique = true)]
public partial class MakineBilesenler
{
    [Key]
    public int BilesenId { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [StringLength(20)]
    public string BilesenKodu { get; set; } = null!;

    [StringLength(100)]
    public string BilesenAdi { get; set; } = null!;

    [StringLength(50)]
    public string? BilesenTuru { get; set; }

    [StringLength(20)]
    public string? Kritiklik { get; set; }

    public int? TahminiOmur { get; set; }

    [StringLength(20)]
    public string? OmurBirimi { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("MakineBilesenler")]
    public virtual Makineler Makine { get; set; } = null!;
}
