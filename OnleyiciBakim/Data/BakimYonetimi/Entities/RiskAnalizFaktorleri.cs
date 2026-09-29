using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("AnalizId", "SiraNo", Name = "UQ_AnalizFaktorleri", IsUnique = true)]
public partial class RiskAnalizFaktorleri
{
    [Key]
    public int FaktorId { get; set; }

    public int AnalizId { get; set; }

    public int SiraNo { get; set; }

    [StringLength(150)]
    public string Faktor { get; set; } = null!;

    [StringLength(100)]
    public string? Deger { get; set; }

    [StringLength(20)]
    public string? Etki { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("AnalizId")]
    [InverseProperty("RiskAnalizFaktorleri")]
    public virtual RiskAnalizleri Analiz { get; set; } = null!;
}
