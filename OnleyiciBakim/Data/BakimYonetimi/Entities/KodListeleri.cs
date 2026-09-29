using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Data.BakimYonetimi.Entities;

[Index("KodGrubu", "Kod", Name = "IX_KodListeleri_KodGrubu_Kod", IsUnique = true)]
public partial class KodListeleri
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string KodGrubu { get; set; } = null!;

    [StringLength(50)]
    public string Kod { get; set; } = null!;

    [StringLength(300)]
    public string? Aciklama { get; set; }

    public int SiraNo { get; set; }
}
