using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("FirmaId", Name = "IX_Subeler_FirmaId")]
[Index(nameof(FirmaId), nameof(SubeKodu), IsUnique = true, Name = "UQ_Subeler_FirmaKodu")]
public partial class Subeler
{
    [Key]
    [StringLength(20)]
    public string SubeId { get; set; } = null!;

    [StringLength(20)]
    public string FirmaId { get; set; } = null!;

    [StringLength(200)]
    public string? SubeAdi { get; set; }

    [StringLength(50)]
    public string? SubeKodu { get; set; }

    public bool AktifMi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    [InverseProperty("Sube")]
    public virtual ICollection<Departmanlar> Departmanlar { get; set; } = new List<Departmanlar>();

    [ForeignKey("FirmaId")]
    [InverseProperty("Subeler")]
    public virtual Firmalar Firma { get; set; } = null!;

    [InverseProperty("Sube")]
    public virtual ICollection<Makineler> Makineler { get; set; } = new List<Makineler>();
}
