using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("SetKodu", Name = "UQ_HibritKararParametreleri", IsUnique = true)]
public partial class HibritKararParametreleri
{
    [Key]
    public int HibritSetId { get; set; }

    [StringLength(20)]
    public string SetKodu { get; set; } = null!;

    [StringLength(150)]
    public string SetAdi { get; set; } = null!;

    [StringLength(100)]
    public string? ModelAdi { get; set; }

    [StringLength(20)]
    public string? ModelVersiyonu { get; set; }

    public int? KuralSetiId { get; set; }

    public int MlAgirligi { get; set; }

    public int KuralAgirligi { get; set; }

    public int? MinGecmisKayit { get; set; }

    public int? ModelGuvenEsigi { get; set; }

    [StringLength(50)]
    public string? EksikVeriYontemi { get; set; }

    public int? VeriYeterlilikOrani { get; set; }

    public int? UyumsuzlukEsigi { get; set; }

    public bool BuyukFarktaEngelle { get; set; }

    public bool ManuelIncelemeGerekli { get; set; }

    public bool OtomatikPlanTaslagi { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [ForeignKey("KuralSetiId")]
    [InverseProperty("HibritKararParametreleri")]
    public virtual RiskKuralSetleri? KuralSeti { get; set; }
}
