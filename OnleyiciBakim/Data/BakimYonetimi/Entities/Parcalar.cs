using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("ParcaKodu", Name = "UQ_Parcalar", IsUnique = true)]
public partial class Parcalar
{
    [Key]
    public int ParcaId { get; set; }

    [StringLength(20)]
    public string ParcaKodu { get; set; } = null!;

    [StringLength(150)]
    public string ParcaAdi { get; set; } = null!;

    [StringLength(50)]
    public string? BilesenTuru { get; set; }

    [StringLength(20)]
    public string? Birim { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? BirimFiyatTl { get; set; }

    public bool Aktif { get; set; }

    [InverseProperty("ParcaKoduNavigation")]
    public virtual ICollection<BakimDegisenParcalar> BakimDegisenParcalar { get; set; } = new List<BakimDegisenParcalar>();
}
