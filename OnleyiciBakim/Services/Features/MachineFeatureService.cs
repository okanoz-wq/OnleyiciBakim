using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Models.Ai;

namespace OnleyiciBakim.Services.Features;

public sealed class MachineFeatureService(
    ApplicationDbContext dbContext,
    Microsoft.Extensions.Options.IOptions<OnleyiciBakim.Options.AlgorithmicRiskOptions> options)
    : IMachineFeatureService
{
    public async Task<MachineFeatureDto> BuildFeaturesAsync(
        string machineId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default)
    {
        var calculationUtc = calculationDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(calculationDate, DateTimeKind.Utc)
            : calculationDate.ToUniversalTime();
        var at = new DateTimeOffset(calculationUtc);
        var ago7 = at.AddDays(-7);
        var ago30 = at.AddDays(-30);
        var ago90 = at.AddDays(-90);

        var machine = await dbContext.Machines.AsNoTracking()
            .Where(x => x.Id == machineId)
            .Select(x => new
            {
                x.Id, x.Age, x.InstallationYear, x.Model, x.Type, x.Criticality,
                x.LastMaintenanceAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{machineId}' kimlikli makine bulunamadı.");

        var lastMaintenance = await dbContext.MaintenanceRecords.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.PerformedAt <= at)
            .MaxAsync(x => (DateTimeOffset?)x.PerformedAt, cancellationToken)
            ?? machine.LastMaintenanceAt;
        var maintenanceCounts = await dbContext.MaintenanceRecords.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.PerformedAt > ago90 && x.PerformedAt <= at)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count30 = group.Count(x => x.PerformedAt > ago30),
                Count90 = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
        var mandatoryOverdue = await dbContext.MaintenanceRecords.AsNoTracking()
            .AnyAsync(x => x.MachineId == machineId && x.NextMaintenanceAt.HasValue &&
                           x.NextMaintenanceAt.Value < at, cancellationToken);

        var failureWindow = dbContext.FailureRecords.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.OccurredAt > ago90 && x.OccurredAt <= at);
        var failureAggregates = await failureWindow
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count30 = group.Count(x => x.OccurredAt > ago30),
                Count90 = group.Count(),
                Downtime30 = group.Where(x => x.OccurredAt > ago30)
                    .Sum(x => x.DowntimeMinutes ?? 0),
                AverageIntervention90 = group.Average(x => (double?)x.InterventionMinutes)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var totalFailures = await dbContext.FailureRecords.AsNoTracking()
            .CountAsync(x => x.MachineId == machineId && x.OccurredAt <= at, cancellationToken);
        var failureTypeCounts = await failureWindow
            .GroupBy(x => x.FailureType)
            .Select(group => group.Count())
            .ToListAsync(cancellationToken);
        var repeatedFailures = failureTypeCounts.Sum(count => Math.Max(0, count - 1));
        var repeatedCritical7Days = await dbContext.FailureRecords.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.OccurredAt > ago7 && x.OccurredAt <= at &&
                        (x.Severity == "Kritik" || x.Severity == "KRITIK" ||
                         x.Severity == "Critical"))
            .GroupBy(x => x.FailureType)
            .AnyAsync(group => group.Count() >= 2, cancellationToken);

        var errorCounts = await dbContext.ErrorRecords.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.OccurredAt > ago30 && x.OccurredAt <= at)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count7 = group.Count(x => x.OccurredAt > ago7),
                Count30 = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
        var telemetryStart = DateOnly.FromDateTime(ago7.UtcDateTime);
        var telemetryEnd = DateOnly.FromDateTime(at.UtcDateTime);
        var telemetry = await dbContext.DailyTelemetry.AsNoTracking()
            .Where(x => x.MachineId == machineId && x.Date >= telemetryStart && x.Date <= telemetryEnd)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Voltage = group.Average(x => (double)x.VoltageAverage),
                Rotation = group.Average(x => (double)x.RotationAverage),
                Pressure = group.Average(x => (double)x.PressureAverage),
                Vibration = group.Average(x => (double)x.VibrationAverage),
                Anomaly = group.Average(x => (double)x.AnomalyScore),
                Production = group.Average(x => (double)x.ProductionIntensityPercent)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var age = machine.Age ?? (machine.InstallationYear.HasValue
            ? Math.Max(0, at.Year - machine.InstallationYear.Value)
            : (int?)null);
        return new MachineFeatureDto
        {
            MachineId = machine.Id,
            Yas = age,
            SonBakimdanGecenGun = lastMaintenance.HasValue
                ? Math.Max(0, (at - lastMaintenance.Value).TotalDays)
                : null,
            BakimSayisi30G = maintenanceCounts?.Count30 ?? 0,
            BakimSayisi90G = maintenanceCounts?.Count90 ?? 0,
            ArizaSayisi30G = failureAggregates?.Count30 ?? 0,
            ArizaSayisi90G = failureAggregates?.Count90 ?? 0,
            ToplamGecmisAriza = totalFailures,
            HataSayisi7G = errorCounts?.Count7 ?? 0,
            HataSayisi30G = errorCounts?.Count30 ?? 0,
            DurusSuresi30G = failureAggregates?.Downtime30 ?? 0,
            OrtMudahaleSuresi90G = failureAggregates?.AverageIntervention90,
            TekrarArizaSayisi90G = repeatedFailures,
            VoltOrt7G = telemetry?.Voltage,
            DevirOrt7G = telemetry?.Rotation,
            BasincOrt7G = telemetry?.Pressure,
            TitresimOrt7G = telemetry?.Vibration,
            AnomaliPuani7G = telemetry?.Anomaly,
            UretimYogunlugu7G = telemetry?.Production,
            Model = NullIfWhiteSpace(machine.Model ?? machine.Type),
            KritikSeviye = NullIfWhiteSpace(machine.Criticality),
            HasCriticalTelemetry = telemetry?.Anomaly >= (double)options.Value.CriticalAnomalyThreshold,
            IsMandatoryMaintenanceOverdue = mandatoryOverdue,
            HasRepeatedCriticalFailure7Days = repeatedCritical7Days
        };
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
