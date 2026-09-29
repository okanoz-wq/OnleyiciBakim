using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

public partial class BakimKontrolSonuclari
{
    [Key]
    public int SonucId { get; set; }

    [StringLength(20)]
    public string BakimId { get; set; } = null!;

    public int? MaddeId { get; set; }

    public int SiraNo { get; set; }

    [StringLength(300)]
    public string KontrolMaddesi { get; set; } = null!;

    [StringLength(50)]
    public string? Sonuc { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("BakimId")]
    [InverseProperty("BakimKontrolSonuclari")]
    public virtual BakimKayitlari Bakim { get; set; } = null!;

    [ForeignKey("MaddeId")]
    [InverseProperty("BakimKontrolSonuclari")]
    public virtual KontrolListesiMaddeleri? Madde { get; set; }
}
