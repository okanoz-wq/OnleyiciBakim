using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Services;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class BakimYonetimiReadModelTests
{
    [Fact]
    public void DeliveredSchema_MapsAllBusinessTables()
    {
        using var context = CreateContext();

        Assert.Equal(44, context.Model.GetEntityTypes().Count());
        Assert.NotNull(context.Model.FindEntityType(typeof(MakineSayacKayitlari)));
        Assert.NotNull(context.Model.FindEntityType(typeof(MakineCalismaTakvimleri)));
        Assert.NotNull(context.Model.FindEntityType(typeof(MakineTransferleri)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ArizaDegisenParcalar)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Ekipler)));
        Assert.NotNull(context.Model.FindEntityType(typeof(IsEmriDurumGecmisi)));
        Assert.NotNull(context.Model.FindEntityType(typeof(UyariIslemGecmisi)));
        Assert.NotNull(context.Model.FindEntityType(typeof(BakimPlanDurumGecmisi)));
    }

    [Fact]
    public async Task Dashboard_AnchorsMonthlyChartToHistoricalDataset()
    {
        await using var context = CreateContext();
        var line = new UretimHatlari
        {
            HatId = "HAT-1",
            DepartmanId = "DEP-1",
            HatAdi = "Hat 1",
            VardiyaSayisi = 3,
            AktifMi = true
        };
        var machine = new Makineler
        {
            MakineId = "MAK-1",
            KaynakMakineId = 1,
            FirmaId = "FIR-1",
            SubeId = "SUB-1",
            DepartmanId = "DEP-1",
            HatId = line.HatId,
            MakineKodu = "M001",
            MakineAdi = "Test Makinesi",
            Yas = 5,
            KurulumYili = 2020,
            SonBakimTarihi = new DateTime(2015, 11, 1),
            GuncelRiskPuani = 72m,
            Hat = line
        };
        context.AddRange(
            line,
            machine,
            new ArizaKartlari
            {
                ArizaId = "ARZ-1",
                ArizaTarihi = new DateTime(2015, 11, 10),
                MakineId = machine.MakineId,
                ArizaTuru = "Motor arızası",
                DurusSuresiDk = 120,
                Makine = machine
            },
            new ArizaKartlari
            {
                ArizaId = "ARZ-2",
                ArizaTarihi = new DateTime(2015, 12, 10),
                MakineId = machine.MakineId,
                ArizaTuru = "Rulman aşınması",
                DurusSuresiDk = 60,
                Makine = machine
            },
            new BakimPlanlari
            {
                PlanId = "PLN-1",
                MakineId = machine.MakineId,
                PlanlananTarih = new DateTime(2016, 1, 10),
                TahminiSureDk = 60,
                TahminiMaliyetTl = 100,
                Durum = "Planlandı",
                Makine = machine
            });
        await context.SaveChangesAsync();

        var service = new BakimYonetimiDashboardService(
            context,
            new RiskClassificationService());
        var result = await service.GetAsync(6, 5, null, CancellationToken.None);

        Assert.Equal(1, result.TotalMachines);
        Assert.Equal(1, result.HighOrCriticalRiskMachines);
        Assert.Equal(2, result.MonthlyFailureAnalysis.Sum(x => x.FailureCount));
        Assert.Equal(1.5m, result.AverageFailureDurationHours);
        Assert.Contains(result.MonthlyFailureAnalysis, x => x.Year == 2015 && x.Month == 12);

        var selectedYear = await service.GetAsync(12, 5, 2015, CancellationToken.None);
        Assert.Equal(12, selectedYear.MonthlyFailureAnalysis.Count);
        Assert.Equal(1, selectedYear.MonthlyFailureAnalysis.First().Month);
        Assert.Equal(12, selectedYear.MonthlyFailureAnalysis.Last().Month);
        Assert.Equal(2, selectedYear.MonthlyFailureAnalysis.Sum(x => x.FailureCount));
    }

    private static BakimYonetimiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BakimYonetimiDbContext>()
            .UseInMemoryDatabase($"bakim-yonetimi-{Guid.NewGuid():N}")
            .Options;
        return new BakimYonetimiDbContext(options);
    }
}
