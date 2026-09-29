using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("ArizaKodu", Name = "UQ_ArizaKoduTanimlari", IsUnique = true)]
public partial class ArizaKoduTanimlari
{
    [Key]
    public int ArizaKoduId { get; set; }

    [StringLength(20)]
    public string ArizaKodu { get; set; } = null!;

    [StringLength(150)]
    public string ArizaAdi { get; set; } = null!;

    [StringLength(50)]
    public string? ArizaKategorisi { get; set; }

    [StringLength(20)]
    public string? VarsayilanOnemSeviyesi { get; set; }

    public int? TekrarKontrolSuresiGun { get; set; }

    [StringLength(50)]
    public string? MakineTuru { get; set; }

    [StringLength(50)]
    public string? IlgiliBilesenTuru { get; set; }

    public bool UretimiDurdururMu { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}
