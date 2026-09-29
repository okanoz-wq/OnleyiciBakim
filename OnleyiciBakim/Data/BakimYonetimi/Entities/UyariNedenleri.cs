using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("UyariId", "SiraNo", Name = "UQ_UyariNedenleri", IsUnique = true)]
public partial class UyariNedenleri
{
    [Key]
    public int NedenId { get; set; }

    [StringLength(20)]
    public string UyariId { get; set; } = null!;

    public int SiraNo { get; set; }

    [StringLength(150)]
    public string RiskNedeni { get; set; } = null!;

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [StringLength(500)]
    public string? OnerilenAksiyon { get; set; }

    [ForeignKey("UyariId")]
    [InverseProperty("UyariNedenleri")]
    public virtual Uyarilar Uyari { get; set; } = null!;
}
