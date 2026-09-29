using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("KontrolListesiKodu", Name = "UQ_BakimKontrolListeleri", IsUnique = true)]
public partial class BakimKontrolListeleri
{
    [Key]
    public int KontrolListesiId { get; set; }

    [StringLength(20)]
    public string KontrolListesiKodu { get; set; } = null!;

    [StringLength(150)]
    public string KontrolListesiAdi { get; set; } = null!;

    [StringLength(20)]
    public string? BilesenKodu { get; set; }

    [StringLength(20)]
    public string? BakimTuruKodu { get; set; }

    [StringLength(100)]
    public string? MakineTuru { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    [ForeignKey("BakimTuruKodu")]
    [InverseProperty("BakimKontrolListeleri")]
    public virtual BakimTuruTanimlari? BakimTuruKoduNavigation { get; set; }

    [InverseProperty("KontrolListesi")]
    public virtual ICollection<KontrolListesiMaddeleri> KontrolListesiMaddeleri { get; set; } = new List<KontrolListesiMaddeleri>();
}
