using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data.BakimYonetimi.Entities;

namespace OnleyiciBakim.Data.BakimYonetimi;

public partial class BakimYonetimiDbContext : DbContext
{
    public BakimYonetimiDbContext(DbContextOptions<BakimYonetimiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ArizaKartlari> ArizaKartlari { get; set; }

    public virtual DbSet<ArizaKoduTanimlari> ArizaKoduTanimlari { get; set; }

    public virtual DbSet<ArizaMudahaleler> ArizaMudahaleler { get; set; }

    public virtual DbSet<BakimDegisenParcalar> BakimDegisenParcalar { get; set; }

    public virtual DbSet<BakimKayitlari> BakimKayitlari { get; set; }

    public virtual DbSet<BakimKontrolListeleri> BakimKontrolListeleri { get; set; }

    public virtual DbSet<BakimKontrolSonuclari> BakimKontrolSonuclari { get; set; }

    public virtual DbSet<BakimOneriParametreleri> BakimOneriParametreleri { get; set; }

    public virtual DbSet<BakimPlanGorevleri> BakimPlanGorevleri { get; set; }

    public virtual DbSet<BakimPlanlari> BakimPlanlari { get; set; }

    public virtual DbSet<BakimTuruTanimlari> BakimTuruTanimlari { get; set; }

    public virtual DbSet<Departmanlar> Departmanlar { get; set; }

    public virtual DbSet<Firmalar> Firmalar { get; set; }

    public virtual DbSet<GuncelRiskler> GuncelRiskler { get; set; }

    public virtual DbSet<HataKayitlari> HataKayitlari { get; set; }

    public virtual DbSet<HibritKararParametreleri> HibritKararParametreleri { get; set; }

    public virtual DbSet<IsEmirleri> IsEmirleri { get; set; }

    public virtual DbSet<KararGecmisi> KararGecmisi { get; set; }

    public virtual DbSet<KodListeleri> KodListeleri { get; set; }

    public virtual DbSet<KontrolListesiMaddeleri> KontrolListesiMaddeleri { get; set; }

    public virtual DbSet<MakineBilesenler> MakineBilesenler { get; set; }

    public virtual DbSet<Makineler> Makineler { get; set; }

    public virtual DbSet<Parcalar> Parcalar { get; set; }

    public virtual DbSet<Personeller> Personeller { get; set; }

    public virtual DbSet<RiskAnalizFaktorleri> RiskAnalizFaktorleri { get; set; }

    public virtual DbSet<RiskAnalizleri> RiskAnalizleri { get; set; }

    public virtual DbSet<RiskKuralDetaylari> RiskKuralDetaylari { get; set; }

    public virtual DbSet<RiskKuralSetleri> RiskKuralSetleri { get; set; }

    public virtual DbSet<Subeler> Subeler { get; set; }

    public virtual DbSet<TelemetriGunluk> TelemetriGunluk { get; set; }

    public virtual DbSet<UretimHatlari> UretimHatlari { get; set; }

    public virtual DbSet<UyariNedenleri> UyariNedenleri { get; set; }

    public virtual DbSet<Uyarilar> Uyarilar { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArizaKartlari>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.ArizaKartlari).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ArizaKoduTanimlari>(entity =>
        {
            entity.HasKey(e => e.ArizaKoduId).HasName("PK__ArizaKod__0F641495E1ABF47F");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<ArizaMudahaleler>(entity =>
        {
            entity.HasKey(e => e.MudahaleId).HasName("PK__ArizaMud__8E894C8AA8557047");

            entity.HasOne(d => d.Ariza).WithMany(p => p.ArizaMudahaleler)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArizaMudahaleler_Ariza");

            entity.HasOne(d => d.Personel).WithMany(p => p.ArizaMudahaleler).HasConstraintName("FK_ArizaMudahaleler_Personel");
        });

        modelBuilder.Entity<BakimDegisenParcalar>(entity =>
        {
            entity.HasKey(e => e.KayitId).HasName("PK__BakimDeg__BD28AF4BAC8805C3");

            entity.Property(e => e.Miktar).HasDefaultValue(1m);

            entity.HasOne(d => d.Bakim).WithMany(p => p.BakimDegisenParcalar)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DegisenParca_Bakim");

            entity.HasOne(d => d.ParcaKoduNavigation).WithMany(p => p.BakimDegisenParcalar)
                .HasPrincipalKey(p => p.ParcaKodu)
                .HasForeignKey(d => d.ParcaKodu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DegisenParca_Parca");
        });

        modelBuilder.Entity<BakimKayitlari>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.BakimKayitlari).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BakimKontrolListeleri>(entity =>
        {
            entity.HasKey(e => e.KontrolListesiId).HasName("PK__BakimKon__37F568CBED2FFFB2");

            entity.Property(e => e.Aktif).HasDefaultValue(true);

            entity.HasOne(d => d.BakimTuruKoduNavigation).WithMany(p => p.BakimKontrolListeleri)
                .HasPrincipalKey(p => p.BakimTuruKodu)
                .HasForeignKey(d => d.BakimTuruKodu)
                .HasConstraintName("FK_KontrolListeleri_BakimTuru");
        });

        modelBuilder.Entity<BakimKontrolSonuclari>(entity =>
        {
            entity.HasKey(e => e.SonucId).HasName("PK__BakimKon__3AF190B89B98B7F4");

            entity.HasOne(d => d.Bakim).WithMany(p => p.BakimKontrolSonuclari)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KontrolSonuc_Bakim");

            entity.HasOne(d => d.Madde).WithMany(p => p.BakimKontrolSonuclari).HasConstraintName("FK_KontrolSonuc_Madde");
        });

        modelBuilder.Entity<BakimOneriParametreleri>(entity =>
        {
            entity.HasKey(e => e.OneriId).HasName("PK__BakimOne__E5ADEA37CDB74F58");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.BildirimGoster).HasDefaultValue(true);
            entity.Property(e => e.OtomatikUyariOlustur).HasDefaultValue(true);
        });

        modelBuilder.Entity<BakimPlanGorevleri>(entity =>
        {
            entity.HasKey(e => e.GorevId).HasName("PK__BakimPla__2BC59969CE4102AF");

            entity.Property(e => e.Durum).HasDefaultValue("Planlandi");

            entity.HasOne(d => d.Plan).WithMany(p => p.BakimPlanGorevleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PlanGorevleri_Plan");

            entity.HasOne(d => d.SorumluPersonel).WithMany(p => p.BakimPlanGorevleri).HasConstraintName("FK_PlanGorevleri_Personel");
        });

        modelBuilder.Entity<BakimPlanlari>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.BakimPlanlari).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<BakimTuruTanimlari>(entity =>
        {
            entity.HasKey(e => e.BakimTuruId).HasName("PK__BakimTur__55A53796FE30507A");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
        });

        modelBuilder.Entity<Departmanlar>(entity =>
        {
            entity.HasOne(d => d.Sube).WithMany(p => p.Departmanlar).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<GuncelRiskler>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.GuncelRiskler).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HataKayitlari>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.HataKayitlari).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<HibritKararParametreleri>(entity =>
        {
            entity.HasKey(e => e.HibritSetId).HasName("PK__HibritKa__49DA628244C86B69");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.KuralAgirligi).HasDefaultValue(30);
            entity.Property(e => e.MlAgirligi).HasDefaultValue(70);

            entity.HasOne(d => d.KuralSeti).WithMany(p => p.HibritKararParametreleri).HasConstraintName("FK_Hibrit_KuralSeti");
        });

        modelBuilder.Entity<IsEmirleri>(entity =>
        {
            entity.HasKey(e => e.IsEmriId).HasName("PK__IsEmirle__C7F2A0923CC127CF");

            entity.Property(e => e.OlusturmaTarihi).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Ariza).WithMany(p => p.IsEmirleri).HasConstraintName("FK_IsEmirleri_Ariza");

            entity.HasOne(d => d.Makine).WithMany(p => p.IsEmirleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IsEmirleri_Makine");

            entity.HasOne(d => d.Plan).WithMany(p => p.IsEmirleri).HasConstraintName("FK_IsEmirleri_Plan");

            entity.HasOne(d => d.SorumluPersonel).WithMany(p => p.IsEmirleri).HasConstraintName("FK_IsEmirleri_Personel");
        });

        modelBuilder.Entity<KararGecmisi>(entity =>
        {
            entity.HasKey(e => e.KararId).HasName("PK__KararGec__D0AFD3EB484F5D31");

            entity.Property(e => e.KararTarihi).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Analiz).WithMany(p => p.KararGecmisi).HasConstraintName("FK_KararGecmisi_Analiz");

            entity.HasOne(d => d.KararVerenPersonel).WithMany(p => p.KararGecmisi).HasConstraintName("FK_KararGecmisi_Personel");

            entity.HasOne(d => d.Makine).WithMany(p => p.KararGecmisi)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KararGecmisi_Makine");
        });

        modelBuilder.Entity<KontrolListesiMaddeleri>(entity =>
        {
            entity.HasKey(e => e.MaddeId).HasName("PK__KontrolL__58BDF80E3B88DB44");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.Zorunlu).HasDefaultValue(true);

            entity.HasOne(d => d.KontrolListesi).WithMany(p => p.KontrolListesiMaddeleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KontrolMaddeleri_Liste");
        });

        modelBuilder.Entity<MakineBilesenler>(entity =>
        {
            entity.HasKey(e => e.BilesenId).HasName("PK__MakineBi__617F25E0D8DCD93F");

            entity.Property(e => e.Aktif).HasDefaultValue(true);

            entity.HasOne(d => d.Makine).WithMany(p => p.MakineBilesenler)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MakineBilesenler_Makineler");
        });

        modelBuilder.Entity<Makineler>(entity =>
        {
            entity.HasIndex(e => e.MakineKodu, "IX_Makineler_MakineKodu")
                .IsUnique()
                .HasFilter("([MakineKodu] IS NOT NULL)");

            entity.HasOne(d => d.Departman).WithMany(p => p.Makineler).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Firma).WithMany(p => p.Makineler).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Hat).WithMany(p => p.Makineler).OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Sube).WithMany(p => p.Makineler).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Parcalar>(entity =>
        {
            entity.HasKey(e => e.ParcaId).HasName("PK__Parcalar__ECA5E73117C61AE3");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.Birim).HasDefaultValue("Adet");
        });

        modelBuilder.Entity<RiskAnalizFaktorleri>(entity =>
        {
            entity.HasKey(e => e.FaktorId).HasName("PK__RiskAnal__33DCB5CD4972F942");

            entity.HasOne(d => d.Analiz).WithMany(p => p.RiskAnalizFaktorleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AnalizFaktorleri_Analiz");
        });

        modelBuilder.Entity<RiskAnalizleri>(entity =>
        {
            entity.HasKey(e => e.AnalizId).HasName("PK__RiskAnal__F0EC43B677CAE82F");

            entity.Property(e => e.AnalizTarihi).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Durum).HasDefaultValue("Tamamlandi");

            entity.HasOne(d => d.KuralSeti).WithMany(p => p.RiskAnalizleri).HasConstraintName("FK_RiskAnalizleri_KuralSeti");

            entity.HasOne(d => d.Makine).WithMany(p => p.RiskAnalizleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RiskAnalizleri_Makine");
        });

        modelBuilder.Entity<RiskKuralDetaylari>(entity =>
        {
            entity.HasKey(e => e.KuralId).HasName("PK__RiskKura__9F746B8A24A97420");

            entity.Property(e => e.Aktif).HasDefaultValue(true);

            entity.HasOne(d => d.KuralSeti).WithMany(p => p.RiskKuralDetaylari)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KuralDetaylari_Set");
        });

        modelBuilder.Entity<RiskKuralSetleri>(entity =>
        {
            entity.HasKey(e => e.KuralSetiId).HasName("PK__RiskKura__CA2E805995A95572");

            entity.Property(e => e.Aktif).HasDefaultValue(true);
            entity.Property(e => e.Versiyon).HasDefaultValue("1.0");
        });

        modelBuilder.Entity<Subeler>(entity =>
        {
            entity.HasOne(d => d.Firma).WithMany(p => p.Subeler).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TelemetriGunluk>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.TelemetriGunluk).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UretimHatlari>(entity =>
        {
            entity.HasOne(d => d.Departman).WithMany(p => p.UretimHatlari).OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UyariNedenleri>(entity =>
        {
            entity.HasKey(e => e.NedenId).HasName("PK__UyariNed__FDF6D205DC385948");

            entity.HasOne(d => d.Uyari).WithMany(p => p.UyariNedenleri)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UyariNedenleri_Uyari");
        });

        modelBuilder.Entity<Uyarilar>(entity =>
        {
            entity.HasOne(d => d.Makine).WithMany(p => p.Uyarilar).OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
