using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("OneriKodu", Name = "UQ_BakimOneriParametreleri", IsUnique = true)]
public partial class BakimOneriParametreleri
{
    [Key]
    public int OneriId { get; set; }

    [StringLength(20)]
    public string OneriKodu { get; set; } = null!;

    [StringLength(20)]
    public string RiskSeviyesi { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal MinPuan { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal MaxPuan { get; set; }

    public bool MaxDahil { get; set; }

    public int OnerilenGun { get; set; }

    [StringLength(50)]
    public string? VarsayilanBakimTuru { get; set; }

    [StringLength(20)]
    public string? VarsayilanOncelik { get; set; }

    public bool OtomatikUyariOlustur { get; set; }

    public bool OtomatikPlanTaslagi { get; set; }

    public bool YoneticiOnayiGerekli { get; set; }

    public bool GerekceZorunlu { get; set; }

    public bool BildirimGoster { get; set; }

    public bool IslemGerekmiyorSecenegi { get; set; }

    [StringLength(50)]
    public string? VarsayilanKarar { get; set; }

    [StringLength(50)]
    public string? SorumluRol { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }
}
