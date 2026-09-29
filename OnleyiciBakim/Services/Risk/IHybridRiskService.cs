using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Risk;

public sealed record HybridRiskCalculation(
    decimal AiRiskScore,
    decimal RuleRiskScore,
    decimal FinalRiskScore,
    decimal AiWeight,
    decimal RuleWeight,
    decimal DataCompletenessRate,
    IReadOnlyList<string> AppliedOverrides,
    IReadOnlyDictionary<string, decimal> CalculationDetails,
    DateTimeOffset CalculatedAt);

public interface IHybridRiskService
{
    HybridRiskCalculation Calculate(
        decimal ruleRiskScore,
        decimal? aiRiskScore,
        MachineFeatureDto features,
        DateTimeOffset calculatedAt);
}
