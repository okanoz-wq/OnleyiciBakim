using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("SubeId", Name = "IX_Departmanlar_SubeId")]
[Index(nameof(SubeId), nameof(DepartmanKodu), IsUnique = true, Name = "UQ_Departmanlar_SubeKodu")]
public partial class Departmanlar
{
    [Key]
    [StringLength(20)]
    public string DepartmanId { get; set; } = null!;

    [StringLength(20)]
    public string SubeId { get; set; } = null!;

    [StringLength(200)]
    public string? DepartmanAdi { get; set; }

    [StringLength(50)]
    public string? DepartmanKodu { get; set; }

    public bool AktifMi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    [InverseProperty("Departman")]
    public virtual ICollection<Makineler> Makineler { get; set; } = new List<Makineler>();

    [ForeignKey("SubeId")]
    [InverseProperty("Departmanlar")]
    public virtual Subeler Sube { get; set; } = null!;

    [InverseProperty("Departman")]
    public virtual ICollection<UretimHatlari> UretimHatlari { get; set; } = new List<UretimHatlari>();
}
