using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

public partial class Personeller
{
    [Key]
    [StringLength(20)]
    public string PersonelId { get; set; } = null!;

    [StringLength(200)]
    public string? AdSoyad { get; set; }

    [StringLength(100)]
    public string? Rol { get; set; }

    [StringLength(100)]
    public string? Uzmanlik { get; set; }

    public bool AktifMi { get; set; }

    [StringLength(50)]
    public string? VeriKaynagi { get; set; }

    [InverseProperty("Personel")]
    public virtual ICollection<ArizaKartlari> ArizaKartlari { get; set; } = new List<ArizaKartlari>();

    [InverseProperty("Personel")]
    public virtual ICollection<ArizaMudahaleler> ArizaMudahaleler { get; set; } = new List<ArizaMudahaleler>();

    [InverseProperty("Personel")]
    public virtual ICollection<BakimKayitlari> BakimKayitlari { get; set; } = new List<BakimKayitlari>();

    [InverseProperty("SorumluPersonel")]
    public virtual ICollection<BakimPlanGorevleri> BakimPlanGorevleri { get; set; } = new List<BakimPlanGorevleri>();

    [InverseProperty("SorumluPersonel")]
    public virtual ICollection<BakimPlanlari> BakimPlanlari { get; set; } = new List<BakimPlanlari>();

    [InverseProperty("SorumluPersonel")]
    public virtual ICollection<IsEmirleri> IsEmirleri { get; set; } = new List<IsEmirleri>();

    [InverseProperty("KararVerenPersonel")]
    public virtual ICollection<KararGecmisi> KararGecmisi { get; set; } = new List<KararGecmisi>();

    [InverseProperty("SorumluPersonel")]
    public virtual ICollection<Uyarilar> Uyarilar { get; set; } = new List<Uyarilar>();
}
