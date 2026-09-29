using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services.Features;

namespace OnleyiciBakim.Services;

public sealed record ReadModelMlConfidenceRequest(
    string MachineId,
    DateTime CalculationDate);

public interface IReadModelMlConfidenceService
{
    Task<IReadOnlyDictionary<string, decimal?>> GetConfidencesAsync(
        IReadOnlyCollection<ReadModelMlConfidenceRequest> requests,
        CancellationToken cancellationToken);
}

public sealed class ReadModelMlConfidenceService(
    IDbContextFactory<BakimYonetimiDbContext> dbContextFactory,
    IMlPredictionClient mlPredictionClient,
    IMemoryCache cache,
    IOptions<HybridRiskOptions> hybridOptions,
    IOptions<AlgorithmicRiskOptions> algorithmicOptions,
    ILogger<ReadModelMlConfidenceService> logger) : IReadModelMlConfidenceService
{
    private static readonly TimeSpan SuccessfulPredictionCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan FailedPredictionCacheDuration = TimeSpan.FromSeconds(15);

    public async Task<IReadOnlyDictionary<string, decimal?>> GetConfidencesAsync(
        IReadOnlyCollection<ReadModelMlConfidenceRequest> requests,
        CancellationToken cancellationToken)
    {
        var distinctRequests = requests
            .Where(x => !string.IsNullOrWhiteSpace(x.MachineId))
            .GroupBy(x => x.MachineId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.CalculationDate).First())
            .ToArray();
        var result = new ConcurrentDictionary<string, ConfidenceCacheEntry>(
            StringComparer.OrdinalIgnoreCase);
        var pending = new List<(ReadModelMlConfidenceRequest Request, string CacheKey)>();

        foreach (var request in distinctRequests)
        {
            var cacheKey = BuildCacheKey(request);
            if (cache.TryGetValue(cacheKey, out ConfidenceCacheEntry? cached) && cached is not null)
                result[request.MachineId] = cached;
            else
                pending.Add((request, cacheKey));
        }

        if (pending.Count == 0)
            return ToResult(result);

        if (!await mlPredictionClient.IsReadyAsync(cancellationToken))
        {
            logger.LogWarning(
                "SQL Server bakım planları için ML güven skoru hesaplanamadı; ML servisi hazır değil.");
            foreach (var item in pending)
                CacheResult(item.Request.MachineId, item.CacheKey, null, result, false);
            return ToResult(result);
        }

        // Each parallel operation owns its DbContext. EF contexts are intentionally never shared
        // across threads, while the independent machines can still be processed efficiently.
        await Parallel.ForEachAsync(
            pending,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 4
            },
            async (item, token) =>
            {
                try
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(token);
                    var featureService = new BakimYonetimiMachineFeatureService(
                        dbContext, algorithmicOptions);
                    var features = await featureService.BuildFeaturesAsync(
                        item.Request.MachineId,
                        DateTime.SpecifyKind(item.Request.CalculationDate, DateTimeKind.Utc),
                        token);
                    if (features.DataCompletenessRate <
                        hybridOptions.Value.MinimumCompletenessThreshold)
                    {
                        logger.LogInformation(
                            "ML güven skoru atlandı; veri tamlığı eşik altında. " +
                            "MachineId={MachineId} Completeness={Completeness}",
                            item.Request.MachineId,
                            features.DataCompletenessRate);
                        CacheResult(item.Request.MachineId, item.CacheKey, null, result, false);
                        return;
                    }

                    var prediction = await mlPredictionClient.PredictAsync(
                        new MlPredictionRequest(item.Request.MachineId, features), token);
                    decimal? confidence = prediction.IsSuccess && prediction.Confidence.HasValue
                        ? Math.Round(Math.Clamp(prediction.Confidence.Value, 0m, 100m), 1)
                        : null;
                    CacheResult(
                        item.Request.MachineId,
                        item.CacheKey,
                        confidence,
                        result,
                        prediction.IsSuccess);
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    logger.LogWarning(exception,
                        "SQL Server makinesi için ML tahmini alınamadı veya özellikler üretilemedi. " +
                        "MachineId={MachineId}",
                        item.Request.MachineId);
                    CacheResult(item.Request.MachineId, item.CacheKey, null, result, false);
                }
            });

        return ToResult(result);
    }

    private void CacheResult(
        string machineId,
        string cacheKey,
        decimal? confidence,
        ConcurrentDictionary<string, ConfidenceCacheEntry> result,
        bool successful)
    {
        result[machineId] = new ConfidenceCacheEntry(confidence);
        cache.Set(
            cacheKey,
            new ConfidenceCacheEntry(confidence),
            successful ? SuccessfulPredictionCacheDuration : FailedPredictionCacheDuration);
    }

    private static string BuildCacheKey(ReadModelMlConfidenceRequest request) =>
        $"read-model-ml-confidence:{request.MachineId}:{request.CalculationDate:O}";

    private static IReadOnlyDictionary<string, decimal?> ToResult(
        ConcurrentDictionary<string, ConfidenceCacheEntry> result) =>
        result.ToDictionary(x => x.Key, x => x.Value.Value, StringComparer.OrdinalIgnoreCase);

    private sealed record ConfidenceCacheEntry(decimal? Value);

}
