using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data.BakimYonetimi.Entities;

namespace OnleyiciBakim.Data.BakimYonetimi;

public partial class BakimYonetimiDbContext
{
    public DbSet<MakineSayacTanimlari> MakineSayacTanimlari => Set<MakineSayacTanimlari>();
    public DbSet<Vardiyalar> Vardiyalar => Set<Vardiyalar>();
    public DbSet<MakineSayacKayitlari> MakineSayacKayitlari => Set<MakineSayacKayitlari>();
    public DbSet<MakineCalismaTakvimleri> MakineCalismaTakvimleri => Set<MakineCalismaTakvimleri>();
    public DbSet<MakineTransferleri> MakineTransferleri => Set<MakineTransferleri>();
    public DbSet<Ekipler> Ekipler => Set<Ekipler>();
    public DbSet<EkipUyeleri> EkipUyeleri => Set<EkipUyeleri>();
    public DbSet<ArizaDegisenParcalar> ArizaDegisenParcalar => Set<ArizaDegisenParcalar>();
    public DbSet<IsEmriDurumGecmisi> IsEmriDurumGecmisi => Set<IsEmriDurumGecmisi>();
    public DbSet<UyariIslemGecmisi> UyariIslemGecmisi => Set<UyariIslemGecmisi>();
    public DbSet<BakimPlanDurumGecmisi> BakimPlanDurumGecmisi => Set<BakimPlanDurumGecmisi>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MakineSayacKayitlari>()
            .Property(x => x.SayacFarki)
            .HasComputedColumnSql("[BitisSayac]-[BaslangicSayac]", stored: true);

        modelBuilder.Entity<MakineSayacKayitlari>().HasOne<Makineler>()
            .WithMany().HasForeignKey(x => x.MakineId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MakineSayacKayitlari>().HasOne<MakineSayacTanimlari>()
            .WithMany().HasForeignKey(x => x.SayacTanimId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MakineSayacKayitlari>().HasOne<Vardiyalar>()
            .WithMany().HasForeignKey(x => x.VardiyaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MakineCalismaTakvimleri>().HasOne<Makineler>()
            .WithMany().HasForeignKey(x => x.MakineId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MakineCalismaTakvimleri>().HasOne<Vardiyalar>()
            .WithMany().HasForeignKey(x => x.VardiyaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EkipUyeleri>().HasOne<Ekipler>()
            .WithMany().HasForeignKey(x => x.EkipId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EkipUyeleri>().HasOne<Personeller>()
            .WithMany().HasForeignKey(x => x.PersonelId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ArizaDegisenParcalar>().HasOne<ArizaMudahaleler>()
            .WithMany().HasForeignKey(x => x.MudahaleId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MakineTransferleri>().HasOne<Makineler>()
            .WithMany().HasForeignKey(x => x.MakineId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<IsEmriDurumGecmisi>().HasOne<IsEmirleri>()
            .WithMany().HasForeignKey(x => x.IsEmriId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UyariIslemGecmisi>().HasOne<Uyarilar>()
            .WithMany().HasForeignKey(x => x.UyariId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BakimPlanDurumGecmisi>().HasOne<BakimPlanlari>()
            .WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
