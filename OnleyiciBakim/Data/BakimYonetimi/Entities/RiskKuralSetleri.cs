using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("KuralSetiKodu", "Versiyon", Name = "UQ_RiskKuralSetleri", IsUnique = true)]
public partial class RiskKuralSetleri
{
    [Key]
    public int KuralSetiId { get; set; }

    [StringLength(20)]
    public string KuralSetiKodu { get; set; } = null!;

    [StringLength(150)]
    public string KuralSetiAdi { get; set; } = null!;

    [StringLength(10)]
    public string Versiyon { get; set; } = null!;

    public DateOnly? GecerlilikBaslangic { get; set; }

    public DateOnly? GecerlilikBitis { get; set; }

    public bool Aktif { get; set; }

    [StringLength(500)]
    public string? Aciklama { get; set; }

    [InverseProperty("KuralSeti")]
    public virtual ICollection<HibritKararParametreleri> HibritKararParametreleri { get; set; } = new List<HibritKararParametreleri>();

    [InverseProperty("KuralSeti")]
    public virtual ICollection<RiskAnalizleri> RiskAnalizleri { get; set; } = new List<RiskAnalizleri>();

    [InverseProperty("KuralSeti")]
    public virtual ICollection<RiskKuralDetaylari> RiskKuralDetaylari { get; set; } = new List<RiskKuralDetaylari>();
}
