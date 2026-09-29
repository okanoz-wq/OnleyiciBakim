using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;

namespace OnleyiciBakim.Services;

public sealed class RiskClassificationService(
    ILogger<RiskClassificationService>? logger = null) : IRiskClassificationService
{
    public RiskLevel Classify(decimal score)
    {
        var rawScore = score;
        score = Math.Clamp(score, 0m, 100m);
        if (score != rawScore)
            logger?.LogWarning("Risk puanı clamp edildi. RawScore={RawScore} ClampedScore={ClampedScore}",
                rawScore, score);

        return score switch
        {
            < 30m => RiskLevel.Low,
            < 60m => RiskLevel.Medium,
            < 80m => RiskLevel.High,
            _ => RiskLevel.Critical
        };
    }

    public string GetLabel(RiskLevel level) => level switch
    {
        RiskLevel.Low => "Düşük",
        RiskLevel.Medium => "Orta",
        RiskLevel.High => "Yüksek",
        RiskLevel.Critical => "Kritik",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    public string GetColorName(RiskLevel level) => level switch
    {
        RiskLevel.Low => "green",
        RiskLevel.Medium => "amber",
        RiskLevel.High => "red",
        RiskLevel.Critical => "dark-red",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    public string GetColorHex(RiskLevel level) => level switch
    {
        RiskLevel.Low => "#16A34A",
        RiskLevel.Medium => "#F59E0B",
        RiskLevel.High => "#EF4444",
        RiskLevel.Critical => "#7F1D1D",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

}
