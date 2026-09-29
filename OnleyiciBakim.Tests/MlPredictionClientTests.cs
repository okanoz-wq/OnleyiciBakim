using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class MlPredictionClientTests
{
    [Fact]
    public async Task HttpClient_SendsNestedSnakeCaseTwentyFeatureContract()
    {
        string? capturedJson = null;
        var handler = new StubHandler(async request =>
        {
            capturedJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "aiRiskScore": 68.4,
                  "failureProbability": 0.684,
                  "modelConfidence": 68.4,
                  "aiRiskLevel": "Yüksek",
                  "dataCompletenessRate": 100,
                  "missingFields": [],
                  "warnings": [],
                  "recommendedActions": [],
                  "ruleBasedExplanation": [],
                  "modelVersion": "rf-v2-time-aware",
                  "predictionDate": "2026-08-05T12:00:00Z"
                }
                """, Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new HttpMlPredictionClient(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new MlServiceOptions
            {
                PredictionEndpoint = "/api/v1/predict", RetryCount = 0
            }),
            NullLogger<HttpMlPredictionClient>.Instance);
        var features = CompleteFeatures();

        var result = await client.PredictAsync(
            new MlPredictionRequest("M-1", features), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(68.4m, result.RiskScore);
        Assert.Equal(68.4m, result.Confidence);
        using var document = JsonDocument.Parse(capturedJson!);
        var root = document.RootElement;
        Assert.Equal("M-1", root.GetProperty("machineId").GetString());
        var featureJson = root.GetProperty("features");
        Assert.Equal(20, featureJson.EnumerateObject().Count());
        Assert.True(featureJson.TryGetProperty("son_bakimdan_gecen_gun", out _));
        Assert.False(featureJson.TryGetProperty("machineId", out _));
    }

    private static MachineFeatureDto CompleteFeatures() => new()
    {
        MachineId = "M-1", Yas = 1, SonBakimdanGecenGun = 2,
        BakimSayisi30G = 1, BakimSayisi90G = 2, ArizaSayisi30G = 1,
        ArizaSayisi90G = 2, ToplamGecmisAriza = 3, HataSayisi7G = 1,
        HataSayisi30G = 2, DurusSuresi30G = 10, OrtMudahaleSuresi90G = 5,
        TekrarArizaSayisi90G = 1, VoltOrt7G = 170, DevirOrt7G = 440,
        BasincOrt7G = 100, TitresimOrt7G = 40, AnomaliPuani7G = 4,
        UretimYogunlugu7G = 60, Model = "model1", KritikSeviye = "Orta"
    };

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => callback(request);
    }
}
