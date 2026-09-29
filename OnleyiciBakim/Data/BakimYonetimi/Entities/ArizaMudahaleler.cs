using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("ArizaId", "MudahaleBaslangic", Name = "IX_ArizaMudahaleler_Ariza")]
public partial class ArizaMudahaleler
{
    [Key]
    public int MudahaleId { get; set; }

    [StringLength(20)]
    public string ArizaId { get; set; } = null!;

    public DateTime MudahaleBaslangic { get; set; }

    public DateTime? MudahaleBitis { get; set; }

    [StringLength(20)]
    public string? PersonelId { get; set; }

    [StringLength(1000)]
    public string? YapilanIslem { get; set; }

    [StringLength(200)]
    public string? DegisenParca { get; set; }

    [StringLength(300)]
    public string? KokNeden { get; set; }

    [StringLength(100)]
    public string? Sonuc { get; set; }

    [StringLength(50)]
    public string? Durum { get; set; }

    public int? GercekDurusSuresiDk { get; set; }

    [StringLength(1000)]
    public string? Aciklama { get; set; }

    [ForeignKey("ArizaId")]
    [InverseProperty("ArizaMudahaleler")]
    public virtual ArizaKartlari Ariza { get; set; } = null!;

    [ForeignKey("PersonelId")]
    [InverseProperty("ArizaMudahaleler")]
    public virtual Personeller? Personel { get; set; }
}
