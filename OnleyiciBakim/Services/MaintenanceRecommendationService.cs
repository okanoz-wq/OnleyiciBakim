using Microsoft.Extensions.Options;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services;

public sealed class MaintenanceRecommendationService(
    IOptions<MaintenanceRecommendationOptions> options) : IMaintenanceRecommendationService
{
    public MaintenanceRecommendation Get(RiskLevel level)
    {
        var settings = options.Value;
        return level switch
        {
            RiskLevel.Low => new(
                settings.LowDays, settings.LowType,
                $"Yaklaşık {settings.LowDays} gün içinde rutin bakım planla.", false),
            RiskLevel.Medium => new(
                settings.MediumDays, settings.MediumType,
                $"{settings.MediumDays} gün içinde planlı bakım oluştur.", false),
            RiskLevel.High => new(
                settings.HighDays, settings.HighType,
                $"{settings.HighDays} gün içinde acil bakım ve teknik kontrol oluştur.", true),
            RiskLevel.Critical => new(
                settings.CriticalDays, settings.CriticalType,
                "Makineyi güvenli duruma al; derhal inceleme ve acil müdahale planla.", true),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }
}
