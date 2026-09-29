using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("KontrolListesiId", "SiraNo", Name = "UQ_KontrolMaddeleri", IsUnique = true)]
public partial class KontrolListesiMaddeleri
{
    [Key]
    public int MaddeId { get; set; }

    public int KontrolListesiId { get; set; }

    public int SiraNo { get; set; }

    [StringLength(300)]
    public string KontrolMaddesi { get; set; } = null!;

    public bool Zorunlu { get; set; }

    public bool AciklamaGerekli { get; set; }

    public bool Aktif { get; set; }

    [InverseProperty("Madde")]
    public virtual ICollection<BakimKontrolSonuclari> BakimKontrolSonuclari { get; set; } = new List<BakimKontrolSonuclari>();

    [ForeignKey("KontrolListesiId")]
    [InverseProperty("KontrolListesiMaddeleri")]
    public virtual BakimKontrolListeleri KontrolListesi { get; set; } = null!;
}
