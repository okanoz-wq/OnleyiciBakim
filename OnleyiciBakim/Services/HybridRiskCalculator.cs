using Microsoft.Extensions.Options;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services;

public sealed class HybridRiskCalculator(
    IOptions<HybridRiskOptions> options,
    ILogger<HybridRiskCalculator>? logger = null) : IHybridRiskCalculator
{
    public decimal Calculate(decimal algorithmicScore, decimal machineLearningScore)
    {
        var settings = options.Value;
        var algorithmic = Math.Clamp(algorithmicScore, 0m, 100m);
        var machineLearning = Math.Clamp(machineLearningScore, 0m, 100m);
        if (algorithmic != algorithmicScore || machineLearning != machineLearningScore)
            logger?.LogWarning(
                "Hibrit risk girdisi clamp edildi. AlgorithmicRaw={AlgorithmicRaw} MlRaw={MlRaw}",
                algorithmicScore, machineLearningScore);
        return Math.Clamp(
            algorithmic * settings.AlgorithmWeight +
            machineLearning * settings.MachineLearningWeight,
            0m,
            100m);
    }
}
