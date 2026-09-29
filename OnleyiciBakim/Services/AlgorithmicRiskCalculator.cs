using System.Globalization;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services;

public sealed class AlgorithmicRiskCalculator(IOptions<AlgorithmicRiskOptions> options)
    : IAlgorithmicRiskCalculator
{
    public AlgorithmicCalculationResult Calculate(AlgorithmicRiskInput input)
    {
        var settings = options.Value;
        var factors = new[]
        {
            Factor("failureFrequency", "Arıza sıklığı", input.FailureCount30Days,
                settings.FailureFrequencyWeight,
                value => Normalize(Convert.ToDecimal(value, CultureInfo.InvariantCulture), settings.FailureCountMaximum),
                "Son 30 gündeki arıza sayısı"),
            Factor("maintenanceAge", "Son bakımdan geçen süre", input.DaysSinceLastMaintenance,
                settings.MaintenanceAgeWeight,
                value => Normalize(Convert.ToDecimal(value, CultureInfo.InvariantCulture), settings.MaintenanceAgeMaximumDays),
                "Son bakımdan geçen gün"),
            Factor("downtime", "Duruş süresi", input.TotalDowntimeHours30Days,
                settings.DowntimeWeight,
                value => Normalize(Convert.ToDecimal(value, CultureInfo.InvariantCulture), settings.DowntimeMaximumHours),
                "Son 30 gündeki toplam duruş saati"),
            Factor("repeatedFailure", "Tekrarlayan arıza",
                input.RepeatedFailureCount90Days.HasValue
                    ? (object)input.RepeatedFailureCount90Days.Value
                    : input.RepeatedFailure,
                settings.RepeatedFailureWeight,
                value => input.RepeatedFailureCount90Days.HasValue
                    ? Normalize(input.RepeatedFailureCount90Days.Value, settings.RepeatedFailureMaximum)
                    : (bool)value ? 100m : 0m,
                "Son 90 gündeki tekrarlayan arıza sayısı"),
            Factor("telemetryAnomaly", "Telemetri/anomali durumu", input.AnomalyScore7Days,
                settings.TelemetryAnomalyWeight,
                value => Normalize(Convert.ToDecimal(value, CultureInfo.InvariantCulture), settings.AnomalyScoreMaximum),
                "Son 7 günlük ortalama anomali puanı"),
            Factor("criticality", "Makine kritiklik seviyesi", input.Criticality,
                settings.CriticalityWeight,
                value => NormalizeCriticality(value?.ToString()),
                "Makinenin işletme kritiklik seviyesi")
        };

        var total = Math.Clamp(factors.Sum(x => x.Contribution), 0m, 100m);
        var completeness = factors.Sum(x => x.IsAvailable ? x.Weight : 0m) * 100m;
        var missing = factors.Where(x => !x.IsAvailable).Select(x => x.DisplayName).ToArray();
        var reasons = factors
            .Where(x => x.IsAvailable && x.NormalizedValue >= 50m)
            .OrderByDescending(x => x.Contribution)
            .Select(x => $"{x.DisplayName}: {x.RawValue}")
            .ToList();
        if (reasons.Count == 0)
            reasons.Add("Belirgin algoritmik risk göstergesi yok");

        var byKey = factors.ToDictionary(x => x.Key);
        var breakdown = new AlgorithmicScoreBreakdown(
            byKey["failureFrequency"].Contribution,
            byKey["maintenanceAge"].Contribution,
            byKey["downtime"].Contribution,
            byKey["repeatedFailure"].Contribution,
            byKey["telemetryAnomaly"].Contribution,
            byKey["criticality"].Contribution,
            total);

        return new AlgorithmicCalculationResult(
            total,
            breakdown,
            reasons,
            Round(completeness),
            missing,
            factors);
    }

    private static RiskFactorResult Factor(
        string key,
        string displayName,
        object? rawValue,
        decimal weight,
        Func<object, decimal> normalize,
        string description)
    {
        var available = rawValue is not null &&
                        (rawValue is not string text || !string.IsNullOrWhiteSpace(text));
        var normalized = available ? Math.Clamp(normalize(rawValue!), 0m, 100m) : 0m;
        return new RiskFactorResult(
            key,
            displayName,
            available ? Convert.ToString(rawValue, CultureInfo.InvariantCulture) : null,
            Round(normalized),
            weight,
            Round(normalized * weight),
            available,
            description);
    }

    private static decimal Normalize(decimal value, decimal maximum) =>
        maximum <= 0m ? 0m : Math.Clamp(value / maximum * 100m, 0m, 100m);

    private static decimal NormalizeCriticality(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "KRİTİK" or "KRITIK" or "CRITICAL" => 100m,
            "YÜKSEK" or "YUKSEK" or "HIGH" => 75m,
            "ORTA" or "MEDIUM" => 50m,
            "DÜŞÜK" or "DUSUK" or "LOW" => 25m,
            _ => 0m
        };

    private static decimal Round(decimal value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
