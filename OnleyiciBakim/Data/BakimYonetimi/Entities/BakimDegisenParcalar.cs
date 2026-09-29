using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

public partial class BakimDegisenParcalar
{
    [Key]
    public int KayitId { get; set; }

    [StringLength(20)]
    public string BakimId { get; set; } = null!;

    [StringLength(20)]
    public string ParcaKodu { get; set; } = null!;

    [Column(TypeName = "decimal(8, 2)")]
    public decimal Miktar { get; set; }

    [StringLength(300)]
    public string? Aciklama { get; set; }

    [ForeignKey("BakimId")]
    [InverseProperty("BakimDegisenParcalar")]
    public virtual BakimKayitlari Bakim { get; set; } = null!;

    [ForeignKey("ParcaKodu")]
    [InverseProperty("BakimDegisenParcalar")]
    public virtual Parcalar ParcaKoduNavigation { get; set; } = null!;
}
