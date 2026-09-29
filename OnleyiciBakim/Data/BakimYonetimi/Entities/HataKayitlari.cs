using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("BagliArizaId", Name = "IX_HataKayitlari_BagliArizaId")]
[Index("MakineId", "HataTarihi", Name = "IX_HataKayitlari_MakineId_HataTarihi")]
public partial class HataKayitlari
{
    [Key]
    [StringLength(20)]
    public string HataId { get; set; } = null!;

    public DateTime HataTarihi { get; set; }

    [StringLength(20)]
    public string MakineId { get; set; } = null!;

    [StringLength(20)]
    public string? HataKodu { get; set; }

    [StringLength(200)]
    public string? HataMesaji { get; set; }

    [StringLength(20)]
    public string? Seviye { get; set; }

    [StringLength(30)]
    public string? Durum { get; set; }

    public DateTime CozulmeTarihi { get; set; }

    [StringLength(20)]
    public string? BagliArizaId { get; set; }

    public bool OtomatikUyariMi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [ForeignKey("BagliArizaId")]
    [InverseProperty("HataKayitlari")]
    public virtual ArizaKartlari? BagliAriza { get; set; }

    [ForeignKey("MakineId")]
    [InverseProperty("HataKayitlari")]
    public virtual Makineler Makine { get; set; } = null!;
}
