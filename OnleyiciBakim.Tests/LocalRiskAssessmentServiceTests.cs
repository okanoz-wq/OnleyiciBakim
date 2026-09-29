using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services;
using OnleyiciBakim.Services.Features;
using OnleyiciBakim.Services.Risk;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class LocalRiskAssessmentServiceTests
{
    [Fact]
    public async Task EvaluateMachine_PersistsRiskWarningAndOnlyOneActiveHybridPlan()
    {
        await using var db = CreateContext();
        Seed(db);
        var service = CreateService(db);

        var first = await service.EvaluateMachineAsync("M-1", "Birim testi", CancellationToken.None);
        var second = await service.EvaluateMachineAsync("M-1", "Tekrar hesaplama", CancellationToken.None);

        Assert.InRange(first.HybridRisk.Score, 0m, 100m);
        Assert.InRange(second.HybridRisk.Score, 0m, 100m);
        Assert.Equal(2, await db.RiskAnalizleri.CountAsync());
        Assert.Single(await db.GuncelRiskler.Where(x => x.MakineId == "M-1").ToListAsync());
        Assert.Single(await db.Uyarilar.Where(x => x.MakineId == "M-1" && x.Durum == "Açık").ToListAsync());
        Assert.Single(await db.BakimPlanlari.Where(x => x.MakineId == "M-1" && x.Durum == "Taslak").ToListAsync());
        Assert.Equal("LOCAL", (await db.GuncelRiskler.SingleAsync()).VeriKaynagi);
    }

    private static LocalRiskAssessmentService CreateService(BakimYonetimiDbContext db)
    {
        var algorithmOptions = Microsoft.Extensions.Options.Options.Create(new AlgorithmicRiskOptions());
        var hybridOptions = Microsoft.Extensions.Options.Options.Create(new HybridRiskOptions());
        var classification = new RiskClassificationService();
        return new LocalRiskAssessmentService(
            db,
            new BakimYonetimiMachineFeatureService(db, algorithmOptions),
            new RuleBasedRiskService(new AlgorithmicRiskCalculator(algorithmOptions)),
            new HybridRiskService(hybridOptions),
            classification,
            new MaintenanceRecommendationService(Microsoft.Extensions.Options.Options.Create(
                new MaintenanceRecommendationOptions())),
            new MockMlPredictionClient(),
            hybridOptions,
            NullLogger<LocalRiskAssessmentService>.Instance);
    }

    private static void Seed(BakimYonetimiDbContext db)
    {
        var company = new Firmalar { FirmaId = "F-1", FirmaKodu = "F", FirmaAdi = "Firma", AktifMi = true };
        var branch = new Subeler { SubeId = "S-1", FirmaId = "F-1", SubeKodu = "S", SubeAdi = "Şube", AktifMi = true };
        var department = new Departmanlar { DepartmanId = "D-1", SubeId = "S-1", DepartmanKodu = "D", DepartmanAdi = "Departman", AktifMi = true };
        var workCenter = new UretimHatlari { HatId = "H-1", DepartmanId = "D-1", HatKodu = "H", HatAdi = "İş Merkezi", VardiyaSayisi = 3, AktifMi = true };
        var machine = new Makineler
        {
            MakineId = "M-1", FirmaId = "F-1", SubeId = "S-1", DepartmanId = "D-1", HatId = "H-1",
            MakineKodu = "M-001", MakineAdi = "Test Makinesi", Model = "T-1", KritikSeviye = "Orta",
            Yas = 4, KurulumYili = DateTime.UtcNow.Year - 4, KurulumTarihi = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4)),
            SonBakimTarihi = DateTime.UtcNow.AddDays(-45), Durum = "Aktif", Aktif = true,
            VeriKaynagi = "LOCAL", OlusturmaTarihi = DateTime.UtcNow
        };
        db.AddRange(company, branch, department, workCenter, machine);
        db.BakimOneriParametreleri.AddRange(
            Recommendation("DUSUK", "Düşük", 0m, 30m, false, 60),
            Recommendation("ORTA", "Orta", 30m, 60m, false, 21),
            Recommendation("YUKSEK", "Yüksek", 60m, 80m, false, 7),
            Recommendation("KRITIK", "Kritik", 80m, 100m, true, 3));
        db.SaveChanges();
    }

    private static BakimOneriParametreleri Recommendation(
        string code, string level, decimal min, decimal max, bool maxIncluded, int days) => new()
    {
        OneriKodu = code, RiskSeviyesi = level, MinPuan = min, MaxPuan = max,
        MaxDahil = maxIncluded, OnerilenGun = days, Aktif = true,
        OtomatikUyariOlustur = true, OtomatikPlanTaslagi = true
    };

    private static BakimYonetimiDbContext CreateContext() => new(
        new DbContextOptionsBuilder<BakimYonetimiDbContext>()
            .UseInMemoryDatabase($"local-risk-{Guid.NewGuid():N}").Options);
}
