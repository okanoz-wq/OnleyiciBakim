using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Integrations.Ml;

public sealed record MlPredictionRequest(
    string MachineId,
    MachineFeatureDto Features,
    string? Scenario = null);

public sealed record MlPredictionResult(
    bool IsSuccess,
    decimal? RiskScore,
    string? ModelName,
    string? ModelVersion,
    string? PredictionId,
    DateTimeOffset? PredictedAtUtc,
    decimal? Confidence,
    IReadOnlyList<string> Warnings,
    string Status,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    decimal? FailureProbability = null,
    decimal? DataCompletenessRate = null,
    IReadOnlyList<string>? MissingFields = null,
    IReadOnlyList<string>? RecommendedActions = null,
    IReadOnlyList<string>? RuleBasedExplanation = null);

public interface IMlPredictionClient
{
    Task<MlPredictionResult> PredictAsync(MlPredictionRequest request, CancellationToken cancellationToken);
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}

public sealed class HttpMlPredictionClient(
    HttpClient httpClient,
    IOptions<MlServiceOptions> options,
    ILogger<HttpMlPredictionClient> logger) : IMlPredictionClient
{
    private readonly MlServiceOptions _options = options.Value;

    public async Task<MlPredictionResult> PredictAsync(
        MlPredictionRequest request,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt <= _options.RetryCount; attempt++)
        {
            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, _options.PredictionEndpoint)
                {
                    Content = JsonContent.Create(new
                    {
                        machineId = request.MachineId,
                        features = request.Features
                    })
                };
                AddApiKey(message);
                using var response = await httpClient.SendAsync(message, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var retryable = response.StatusCode is HttpStatusCode.RequestTimeout or
                        HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError;
                    if (retryable && attempt < _options.RetryCount)
                        continue;
                    return Failed($"http-{(int)response.StatusCode}",
                        $"ML servisi HTTP {(int)response.StatusCode} döndürdü.");
                }

                var payload = await response.Content.ReadFromJsonAsync<MlApiResponse>(
                    cancellationToken: cancellationToken);
                if (payload is null)
                    return Failed("invalid-json", "ML servisi boş veya geçersiz JSON döndürdü.");
                if (!payload.AiRiskScore.HasValue || payload.AiRiskScore is < 0m or > 100m)
                    return Failed("invalid-score", "ML risk puanı 0-100 aralığında değil.");
                if (!payload.DataCompletenessRate.HasValue ||
                    payload.DataCompletenessRate is < 0m or > 100m)
                    return Failed("invalid-completeness",
                        "ML veri tamlık oranı 0-100 aralığında değil.");
                if (!payload.FailureProbability.HasValue ||
                    payload.FailureProbability is < 0m or > 1m)
                    return Failed("invalid-probability",
                        "ML failureProbability alanı 0-1 aralığında değil.");
                var modelConfidence = payload.ModelConfidence ?? Math.Round(
                    Math.Max(payload.FailureProbability.Value, 1m - payload.FailureProbability.Value) * 100m,
                    1);
                if (modelConfidence is < 0m or > 100m)
                    return Failed("invalid-confidence",
                        "ML modelConfidence alanı 0-100 aralığında değil.");

                return new MlPredictionResult(
                    true, payload.AiRiskScore, "RandomForest", payload.ModelVersion,
                    null, payload.PredictionDate, modelConfidence,
                    payload.Warnings ?? [], "Success",
                    FailureProbability: payload.FailureProbability,
                    DataCompletenessRate: payload.DataCompletenessRate,
                    MissingFields: payload.MissingFields ?? [],
                    RecommendedActions: payload.RecommendedActions ?? [],
                    RuleBasedExplanation: payload.RuleBasedExplanation ?? []);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException("ML isteği zaman aşımına uğradı.");
            }
            catch (HttpRequestException exception)
            {
                lastException = exception;
            }
            catch (JsonException exception)
            {
                return Failed("invalid-json", exception.Message);
            }

            if (attempt < _options.RetryCount)
                await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), cancellationToken);
        }

        logger.LogWarning(lastException, "ML tahmini alınamadı. MachineId={MachineId}", request.MachineId);
        return Failed(lastException is TimeoutException ? "timeout" : "unavailable",
            "ML servisine ulaşılamadı.");
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/health");
            AddApiKey(message);
            using var response = await httpClient.SendAsync(message, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void AddApiKey(HttpRequestMessage message)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            message.Headers.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
    }

    private static MlPredictionResult Failed(string code, string message) =>
        new(false, null, null, null, null, null, null, [], "Failed", code, message);

    private sealed class MlApiResponse
    {
        public decimal? AiRiskScore { get; init; }
        public decimal? FailureProbability { get; init; }
        public decimal? ModelConfidence { get; init; }
        public string? AiRiskLevel { get; init; }
        public string? ModelVersion { get; init; }
        public DateTimeOffset? PredictionDate { get; init; }
        public decimal? DataCompletenessRate { get; init; }
        public string[]? MissingFields { get; init; }
        public string[]? Warnings { get; init; }
        public string[]? RecommendedActions { get; init; }
        public string[]? RuleBasedExplanation { get; init; }
    }
}

public sealed class MockMlPredictionClient : IMlPredictionClient
{
    public async Task<MlPredictionResult> PredictAsync(
        MlPredictionRequest request,
        CancellationToken cancellationToken)
    {
        switch (request.Scenario?.Trim().ToLowerInvariant())
        {
            case "timeout":
                await Task.Delay(50, cancellationToken);
                return new(false, null, null, null, null, null, null, [], "Failed",
                    "timeout", "Simüle edilmiş ML timeout.");
            case "unavailable":
                return new(false, null, null, null, null, null, null, [], "Failed",
                    "unavailable", "Simüle edilmiş ML servis kesintisi.");
            case "invalid-score":
                return new(false, null, "MockRiskModel", "dev-1", null, null, null, [], "Failed",
                    "invalid-score", "Simüle edilmiş geçersiz skor.");
        }

        var failures = Convert.ToDecimal(request.Features.ArizaSayisi30G ?? 0d);
        var maintenanceDays = Convert.ToDecimal(request.Features.SonBakimdanGecenGun ?? 0d);
        var downtime = Convert.ToDecimal((request.Features.DurusSuresi30G ?? 0d) / 60d);
        var anomaly = Convert.ToDecimal(request.Features.AnomaliPuani7G ?? 0d);
        var score = Math.Clamp(
            failures * 8m + maintenanceDays / 3m + downtime * 1.5m + anomaly * 0.20m,
            0m,
            100m);
        return new(true, score, "MockRiskModel", "dev-1.0",
            $"mock-{Guid.NewGuid():N}", DateTimeOffset.UtcNow, 0.86m, [], "Success",
            FailureProbability: score / 100m,
            DataCompletenessRate: request.Features.DataCompletenessRate,
            MissingFields: [], RecommendedActions: [], RuleBasedExplanation: []);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

}
