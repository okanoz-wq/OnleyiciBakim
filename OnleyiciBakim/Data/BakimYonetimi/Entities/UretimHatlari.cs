using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("DepartmanId", Name = "IX_UretimHatlari_DepartmanId")]
[Index(nameof(DepartmanId), nameof(HatKodu), IsUnique = true, Name = "UQ_UretimHatlari_DepartmanKodu")]
public partial class UretimHatlari
{
    [Key]
    [StringLength(20)]
    public string HatId { get; set; } = null!;

    [StringLength(20)]
    public string DepartmanId { get; set; } = null!;

    [StringLength(200)]
    public string? HatAdi { get; set; }

    [StringLength(50)]
    public string? HatKodu { get; set; }

    public int VardiyaSayisi { get; set; }

    public bool AktifMi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [StringLength(500)] public string? Aciklama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];

    [ForeignKey("DepartmanId")]
    [InverseProperty("UretimHatlari")]
    public virtual Departmanlar Departman { get; set; } = null!;

    [InverseProperty("Hat")]
    public virtual ICollection<Makineler> Makineler { get; set; } = new List<Makineler>();
}
