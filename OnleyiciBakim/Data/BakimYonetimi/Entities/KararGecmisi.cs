using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("MakineId", "KararTarihi", Name = "IX_KararGecmisi_Makine", IsDescending = new[] { false, true })]
[Index("KararNo", Name = "UQ_KararGecmisi", IsUnique = true)]
public partial class KararGecmisi
{
    [Key]
    public int KararId { get; set; }

    [StringLength(25)]
    public string KararNo { get; set; } = null!;

    public int? AnalizId { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    public DateTime KararTarihi { get; set; }

    [StringLength(100)]
    public string? SistemOnerisi { get; set; }

    [StringLength(100)]
    public string? KullaniciKarari { get; set; }

    [StringLength(20)]
    public string? KararVerenPersonelId { get; set; }

    public bool? Uyumlu { get; set; }

    [StringLength(500)]
    public string? DegisiklikGerekcesi { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("AnalizId")]
    [InverseProperty("KararGecmisi")]
    public virtual RiskAnalizleri? Analiz { get; set; }

    [ForeignKey("KararVerenPersonelId")]
    [InverseProperty("KararGecmisi")]
    public virtual Personeller? KararVerenPersonel { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("KararGecmisi")]
    public virtual Makineler Makine { get; set; } = null!;
}
