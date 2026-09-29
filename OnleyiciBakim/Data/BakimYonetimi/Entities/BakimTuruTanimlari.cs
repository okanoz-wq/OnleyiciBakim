using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("BakimTuruKodu", Name = "UQ_BakimTuruTanimlari", IsUnique = true)]
public partial class BakimTuruTanimlari
{
    [Key]
    public int BakimTuruId { get; set; }

    [StringLength(20)]
    public string BakimTuruKodu { get; set; } = null!;

    [StringLength(100)]
    public string BakimTuruAdi { get; set; } = null!;

    [StringLength(20)]
    public string? Tur { get; set; }

    [StringLength(20)]
    public string? PlanTuru { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? VarsayilanSureSaat { get; set; }

    [StringLength(20)]
    public string? VarsayilanOncelik { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [InverseProperty("BakimTuruKoduNavigation")]
    public virtual ICollection<BakimKontrolListeleri> BakimKontrolListeleri { get; set; } = new List<BakimKontrolListeleri>();
}
