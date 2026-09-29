using Microsoft.Extensions.Options;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services.Risk;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class HybridRiskServiceTests
{
    private readonly HybridRiskService _service = new(
        Microsoft.Extensions.Options.Options.Create(new HybridRiskOptions()));

    [Fact]
    public void HighCompleteness_UsesFortyPercentAi()
    {
        var result = _service.Calculate(50m, 100m, CompleteFeatures(), DateTimeOffset.UtcNow);
        Assert.Equal(0.40m, result.AiWeight);
        Assert.Equal(0.60m, result.RuleWeight);
        Assert.Equal(70m, result.FinalRiskScore);
    }

    [Fact]
    public void MediumCompleteness_UsesTwentyPercentAi()
    {
        var features = CompleteFeatures();
        features.VoltOrt7G = null;
        features.DevirOrt7G = null;
        features.BasincOrt7G = null;
        features.TitresimOrt7G = null;
        features.Model = null;

        var result = _service.Calculate(50m, 100m, features, DateTimeOffset.UtcNow);
        Assert.Equal(75m, result.DataCompletenessRate);
        Assert.Equal(0.20m, result.AiWeight);
        Assert.Equal(0.80m, result.RuleWeight);
        Assert.Equal(60m, result.FinalRiskScore);
    }

    [Fact]
    public void LowCompleteness_UsesOnlyRuleScore()
    {
        var features = new MachineFeatureDto
        {
            MachineId = "M-1", Yas = 1, ArizaSayisi30G = 1,
            KritikSeviye = "Orta", Model = "model1"
        };
        var result = _service.Calculate(47m, 99m, features, DateTimeOffset.UtcNow);
        Assert.Equal(0m, result.AiWeight);
        Assert.Equal(1m, result.RuleWeight);
        Assert.Equal(47m, result.FinalRiskScore);
    }

    [Fact]
    public void CriticalTelemetry_OverridesFinalScoreToAtLeastEighty()
    {
        var features = CompleteFeatures();
        features.HasCriticalTelemetry = true;
        var result = _service.Calculate(10m, 10m, features, DateTimeOffset.UtcNow);
        Assert.Equal(80m, result.FinalRiskScore);
        Assert.Contains(result.AppliedOverrides, item => item.Contains("telemetri"));
    }

    private static MachineFeatureDto CompleteFeatures() => new()
    {
        MachineId = "M-1",
        Yas = 5,
        SonBakimdanGecenGun = 10,
        BakimSayisi30G = 1,
        BakimSayisi90G = 2,
        ArizaSayisi30G = 1,
        ArizaSayisi90G = 2,
        ToplamGecmisAriza = 3,
        HataSayisi7G = 1,
        HataSayisi30G = 2,
        DurusSuresi30G = 30,
        OrtMudahaleSuresi90G = 20,
        TekrarArizaSayisi90G = 1,
        VoltOrt7G = 170,
        DevirOrt7G = 440,
        BasincOrt7G = 100,
        TitresimOrt7G = 40,
        AnomaliPuani7G = 4,
        UretimYogunlugu7G = 60,
        Model = "model1",
        KritikSeviye = "Orta"
    };
}
