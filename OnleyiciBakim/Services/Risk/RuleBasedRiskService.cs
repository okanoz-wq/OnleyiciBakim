using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Risk;

public sealed class RuleBasedRiskService(IAlgorithmicRiskCalculator calculator)
    : IRuleBasedRiskService
{
    public AlgorithmicCalculationResult Calculate(MachineFeatureDto features) =>
        calculator.Calculate(new AlgorithmicRiskInput
        {
            DaysSinceLastMaintenance = ToInt(features.SonBakimdanGecenGun),
            FailureCount30Days = ToInt(features.ArizaSayisi30G),
            TotalDowntimeHours30Days = features.DurusSuresi30G.HasValue
                ? (decimal?)features.DurusSuresi30G.Value / 60m
                : null,
            RepeatedFailureCount90Days = ToInt(features.TekrarArizaSayisi90G),
            RepeatedFailure = features.TekrarArizaSayisi90G.HasValue
                ? features.TekrarArizaSayisi90G.Value > 0
                : null,
            Criticality = features.KritikSeviye,
            AnomalyScore7Days = features.AnomaliPuani7G.HasValue
                ? (decimal?)features.AnomaliPuani7G.Value
                : null,
            ProductionIntensity7Days = features.UretimYogunlugu7G.HasValue
                ? (decimal?)features.UretimYogunlugu7G.Value
                : null
        });

    private static int? ToInt(double? value) =>
        value.HasValue ? Math.Max(0, (int)Math.Round(value.Value)) : null;
}
