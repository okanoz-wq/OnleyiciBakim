using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class RiskClassificationServiceTests
{
    private readonly RiskClassificationService _service = new();

    [Theory]
    [InlineData(0, RiskLevel.Low)]
    [InlineData(29, RiskLevel.Low)]
    [InlineData(29.99, RiskLevel.Low)]
    [InlineData(29.999, RiskLevel.Low)]
    [InlineData(30, RiskLevel.Medium)]
    [InlineData(59.99, RiskLevel.Medium)]
    [InlineData(59.999, RiskLevel.Medium)]
    [InlineData(60, RiskLevel.High)]
    [InlineData(79.99, RiskLevel.High)]
    [InlineData(79.999, RiskLevel.High)]
    [InlineData(80, RiskLevel.Critical)]
    [InlineData(100, RiskLevel.Critical)]
    public void Classify_UsesExactRequiredBoundaries(decimal score, RiskLevel expected)
    {
        Assert.Equal(expected, _service.Classify(score));
    }

    [Theory]
    [InlineData(-0.01, RiskLevel.Low)]
    [InlineData(100.01, RiskLevel.Critical)]
    public void Classify_ClampsOutOfRangeScores(decimal score, RiskLevel expected)
    {
        Assert.Equal(expected, _service.Classify(score));
    }

    [Fact]
    public void Presentation_UsesRequiredLabelsAndColors()
    {
        Assert.Equal(("Düşük", "green"), (_service.GetLabel(RiskLevel.Low), _service.GetColorName(RiskLevel.Low)));
        Assert.Equal(("Orta", "amber"), (_service.GetLabel(RiskLevel.Medium), _service.GetColorName(RiskLevel.Medium)));
        Assert.Equal(("Yüksek", "red"), (_service.GetLabel(RiskLevel.High), _service.GetColorName(RiskLevel.High)));
        Assert.Equal(("Kritik", "dark-red"), (_service.GetLabel(RiskLevel.Critical), _service.GetColorName(RiskLevel.Critical)));
    }
}
