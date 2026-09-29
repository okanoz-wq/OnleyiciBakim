using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services;
using OnleyiciBakim.Services.Features;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class ReadModelMlConfidenceServiceTests
{
    [Fact]
    public async Task MissingStoredPrediction_IsCalculatedAndCached()
    {
        var options = CreateOptions();
        await using var context = new BakimYonetimiDbContext(options);
        var calculationDate = new DateTime(2015, 12, 31, 6, 0, 0, DateTimeKind.Utc);
        context.Makineler.Add(CreateMachine(calculationDate));
        await context.SaveChangesAsync();
        var mlClient = new RecordingMlClient(isReady: true, confidence: 73.4m);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(options, mlClient, cache);
        var request = new[] { new ReadModelMlConfidenceRequest("MAK-ML-1", calculationDate) };

        var first = await service.GetConfidencesAsync(request, CancellationToken.None);
        var second = await service.GetConfidencesAsync(request, CancellationToken.None);

        Assert.Equal(73.4m, first["MAK-ML-1"]);
        Assert.Equal(73.4m, second["MAK-ML-1"]);
        Assert.Equal(1, mlClient.PredictCallCount);
        Assert.Equal(1, mlClient.ReadyCallCount);
    }

    [Fact]
    public async Task UnavailableMlService_ReturnsNullWithoutPredictionAttempt()
    {
        var options = CreateOptions();
        await using var context = new BakimYonetimiDbContext(options);
        var calculationDate = new DateTime(2015, 12, 31, 6, 0, 0, DateTimeKind.Utc);
        context.Makineler.Add(CreateMachine(calculationDate));
        await context.SaveChangesAsync();
        var mlClient = new RecordingMlClient(isReady: false, confidence: 80m);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = CreateService(options, mlClient, cache);

        var result = await service.GetConfidencesAsync(
            [new ReadModelMlConfidenceRequest("MAK-ML-1", calculationDate)],
            CancellationToken.None);

        Assert.Null(result["MAK-ML-1"]);
        Assert.Equal(0, mlClient.PredictCallCount);
        Assert.Equal(1, mlClient.ReadyCallCount);
    }

    private static ReadModelMlConfidenceService CreateService(
        DbContextOptions<BakimYonetimiDbContext> options,
        IMlPredictionClient mlClient,
        IMemoryCache cache)
    {
        var algorithmOptions = Microsoft.Extensions.Options.Options.Create(
            new AlgorithmicRiskOptions());
        return new ReadModelMlConfidenceService(
            new TestDbContextFactory(options),
            mlClient,
            cache,
            Microsoft.Extensions.Options.Options.Create(new HybridRiskOptions()),
            algorithmOptions,
            NullLogger<ReadModelMlConfidenceService>.Instance);
    }

    private static DbContextOptions<BakimYonetimiDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<BakimYonetimiDbContext>()
            .UseInMemoryDatabase($"read-model-ml-{Guid.NewGuid():N}")
            .Options;

    private static Makineler CreateMachine(DateTime calculationDate) => new()
    {
        MakineId = "MAK-ML-1",
        KaynakMakineId = 1,
        FirmaId = "FIR-1",
        SubeId = "SUB-1",
        DepartmanId = "DEP-1",
        HatId = "HAT-1",
        MakineKodu = "M001",
        MakineAdi = "ML Test Makinesi",
        Model = "model3",
        Yas = 8,
        KritikSeviye = "Yüksek",
        KurulumYili = 2007,
        SonBakimTarihi = calculationDate.AddDays(-40),
        GuncelRiskPuani = 65m
    };

    private sealed class RecordingMlClient(bool isReady, decimal confidence) : IMlPredictionClient
    {
        public int PredictCallCount { get; private set; }
        public int ReadyCallCount { get; private set; }

        public Task<MlPredictionResult> PredictAsync(
            MlPredictionRequest request,
            CancellationToken cancellationToken)
        {
            PredictCallCount++;
            return Task.FromResult(new MlPredictionResult(
                true,
                68m,
                "TestModel",
                "test-v1",
                "prediction-1",
                DateTimeOffset.UtcNow,
                confidence,
                [],
                "Success",
                FailureProbability: 0.68m,
                DataCompletenessRate: request.Features.DataCompletenessRate));
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken)
        {
            ReadyCallCount++;
            return Task.FromResult(isReady);
        }
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<BakimYonetimiDbContext> options)
        : IDbContextFactory<BakimYonetimiDbContext>
    {
        public BakimYonetimiDbContext CreateDbContext() => new(options);

        public Task<BakimYonetimiDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
