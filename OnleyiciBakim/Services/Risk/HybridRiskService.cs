using Microsoft.Extensions.Options;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services.Risk;

public sealed class HybridRiskService(IOptions<HybridRiskOptions> options) : IHybridRiskService
{
    public HybridRiskCalculation Calculate(
        decimal ruleRiskScore,
        decimal? aiRiskScore,
        MachineFeatureDto features,
        DateTimeOffset calculatedAt)
    {
        var settings = options.Value;
        var completeness = features.DataCompletenessRate;
        var boundedRule = Math.Clamp(ruleRiskScore, 0m, 100m);
        var boundedAi = Math.Clamp(aiRiskScore ?? 0m, 0m, 100m);
        decimal aiWeight;
        decimal ruleWeight;
        if (!aiRiskScore.HasValue || completeness < settings.MinimumCompletenessThreshold)
        {
            aiWeight = 0m;
            ruleWeight = 1m;
        }
        else if (completeness >= settings.HighCompletenessThreshold)
        {
            aiWeight = settings.HighCompletenessAiWeight;
            ruleWeight = settings.HighCompletenessRuleWeight;
        }
        else
        {
            aiWeight = settings.MediumCompletenessAiWeight;
            ruleWeight = settings.MediumCompletenessRuleWeight;
        }

        var weightedScore = Math.Clamp(
            boundedAi * aiWeight + boundedRule * ruleWeight, 0m, 100m);
        var finalScore = weightedScore;
        var overrides = new List<string>();
        if (features.HasCriticalTelemetry && finalScore < 80m)
        {
            finalScore = 80m;
            overrides.Add("Kritik telemetri eşiği aşıldı; nihai skor en az 80 yapıldı.");
        }
        if (features.IsMandatoryMaintenanceOverdue && finalScore < 60m)
        {
            finalScore = 60m;
            overrides.Add("Zorunlu bakım tarihi geçti; nihai skor en az 60 yapıldı.");
        }
        if (features.HasRepeatedCriticalFailure7Days && finalScore < 80m)
        {
            finalScore = 80m;
            overrides.Add("Son 7 günde kritik arıza tekrarlandı; nihai skor en az 80 yapıldı.");
        }

        return new HybridRiskCalculation(
            boundedAi,
            boundedRule,
            Math.Clamp(finalScore, 0m, 100m),
            aiWeight,
            ruleWeight,
            completeness,
            overrides,
            new Dictionary<string, decimal>
            {
                ["weightedScoreBeforeOverrides"] = weightedScore,
                ["aiContribution"] = boundedAi * aiWeight,
                ["ruleContribution"] = boundedRule * ruleWeight
            },
            calculatedAt);
    }
}
