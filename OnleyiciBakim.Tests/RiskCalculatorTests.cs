using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class RiskCalculatorTests
{
    private static readonly HybridRiskOptions HybridSettings = new()
    {
        AlgorithmWeight = 0.30m,
        MachineLearningWeight = 0.70m
    };

    private static readonly AlgorithmicRiskOptions AlgorithmSettings = new();

    [Theory]
    [InlineData(100, 0, 30)]
    [InlineData(0, 100, 70)]
    [InlineData(50, 50, 50)]
    [InlineData(33.33, 77.77, 64.438)]
    public void HybridCalculator_UsesConfiguredThirtySeventyWeightsWithoutIntermediateRounding(
        decimal algorithmic,
        decimal machineLearning,
        decimal expected)
    {
        var calculator = new HybridRiskCalculator(
            Microsoft.Extensions.Options.Options.Create(HybridSettings));
        Assert.Equal(expected, calculator.Calculate(algorithmic, machineLearning));
    }

    [Theory]
    [InlineData(-10, 100, 70)]
    [InlineData(100, 110, 100)]
    public void HybridCalculator_ClampsRawInputs(decimal algorithmic, decimal ml, decimal expected)
    {
        var calculator = new HybridRiskCalculator(
            Microsoft.Extensions.Options.Options.Create(HybridSettings));
        Assert.Equal(expected, calculator.Calculate(algorithmic, ml));
    }

    [Fact]
    public void AlgorithmicCalculator_ProducesBoundedExplainableScore()
    {
        var calculator = new AlgorithmicRiskCalculator(
            Microsoft.Extensions.Options.Options.Create(AlgorithmSettings));
        var result = calculator.Calculate(new AlgorithmicRiskInput
        {
            DaysSinceLastMaintenance = 365,
            FailureCount30Days = 10,
            TotalDowntimeHours30Days = 100,
            RepeatedFailure = true,
            RepeatedFailureCount90Days = 10,
            Criticality = "Kritik",
            AnomalyScore7Days = 10
        });

        Assert.Equal(100m, result.Score);
        Assert.Equal(result.Score, result.Breakdown.Total);
        Assert.Equal(100m, result.DataCompletenessPercent);
        Assert.Empty(result.MissingFactors);
        Assert.Equal(6, result.Factors.Count);
    }

    [Fact]
    public void AlgorithmicCalculator_DoesNotRedistributeMissingWeights()
    {
        var calculator = new AlgorithmicRiskCalculator(
            Microsoft.Extensions.Options.Options.Create(AlgorithmSettings));
        var result = calculator.Calculate(new AlgorithmicRiskInput
        {
            FailureCount30Days = 5,
            Criticality = "Kritik"
        });

        Assert.Equal(35m, result.Score);
        Assert.Equal(35m, result.DataCompletenessPercent);
        Assert.Equal(4, result.MissingFactors.Count);
    }
}
