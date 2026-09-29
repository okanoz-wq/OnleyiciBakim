using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services.Features;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class MachineFeatureServiceTests
{
    [Fact]
    public async Task BuildFeatures_CalculatesAllTwentyFeaturesAndDateWindows()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"features-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(dbOptions);
        var at = new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
        db.Machines.Add(new Machine
        {
            Id = "M-1", Code = "M-1", Name = "Test", Model = "model2", Age = 8,
            Criticality = "Yüksek", Status = "Aktif"
        });
        db.MaintenanceRecords.AddRange(
            Maintenance("B-1", at.AddDays(-10)),
            Maintenance("B-2", at.AddDays(-40)));
        db.FailureRecords.AddRange(
            Failure("A-1", at.AddDays(-5), "Rulman", 60, 20),
            Failure("A-2", at.AddDays(-20), "Rulman", 120, 40),
            Failure("A-3", at.AddDays(-70), "Elektrik", 30, 60));
        db.ErrorRecords.AddRange(
            Error("H-1", at.AddDays(-3)), Error("H-2", at.AddDays(-10)));
        db.DailyTelemetry.AddRange(
            Telemetry(1, at.AddDays(-1), 170, 440, 100, 40, 4, 60),
            Telemetry(2, at.AddDays(-5), 174, 450, 102, 42, 6, 70));
        await db.SaveChangesAsync();

        var service = new MachineFeatureService(db,
            Microsoft.Extensions.Options.Options.Create(new AlgorithmicRiskOptions()));
        var result = await service.BuildFeaturesAsync("M-1", at.UtcDateTime);

        Assert.Equal(8, result.Yas);
        Assert.Equal(10, result.SonBakimdanGecenGun);
        Assert.Equal(1, result.BakimSayisi30G);
        Assert.Equal(2, result.BakimSayisi90G);
        Assert.Equal(2, result.ArizaSayisi30G);
        Assert.Equal(3, result.ArizaSayisi90G);
        Assert.Equal(3, result.ToplamGecmisAriza);
        Assert.Equal(1, result.HataSayisi7G);
        Assert.Equal(2, result.HataSayisi30G);
        Assert.Equal(180, result.DurusSuresi30G);
        Assert.Equal(40, result.OrtMudahaleSuresi90G);
        Assert.Equal(1, result.TekrarArizaSayisi90G);
        Assert.Equal(172, result.VoltOrt7G);
        Assert.Equal(445, result.DevirOrt7G);
        Assert.Equal(101, result.BasincOrt7G);
        Assert.Equal(41, result.TitresimOrt7G);
        Assert.Equal(5, result.AnomaliPuani7G);
        Assert.Equal(65, result.UretimYogunlugu7G);
        Assert.Equal("model2", result.Model);
        Assert.Equal("Yüksek", result.KritikSeviye);
        Assert.Equal(100m, result.DataCompletenessRate);

        MaintenanceRecord Maintenance(string id, DateTimeOffset date) => new()
        {
            Id = id, MachineId = "M-1", PerformedAt = date,
            MaintenanceType = "Periyodik", DurationMinutes = 30,
            NextMaintenanceAt = at.AddDays(30)
        };
        FailureRecord Failure(
            string id, DateTimeOffset date, string type, int downtime, int intervention) => new()
        {
            Id = id, MachineId = "M-1", OccurredAt = date, FailureType = type,
            Severity = "Orta", DowntimeMinutes = downtime,
            InterventionMinutes = intervention, Status = RecordStatus.Closed
        };
        ErrorRecord Error(string id, DateTimeOffset date) => new()
        {
            Id = id, MachineId = "M-1", OccurredAt = date,
            ErrorCode = id, Severity = "Orta"
        };
        DailyTelemetry Telemetry(
            long id, DateTimeOffset date, decimal voltage, decimal rotation,
            decimal pressure, decimal vibration, decimal anomaly, decimal production) => new()
        {
            Id = id, MachineId = "M-1", Date = DateOnly.FromDateTime(date.UtcDateTime),
            VoltageAverage = voltage, RotationAverage = rotation,
            PressureAverage = pressure, VibrationAverage = vibration,
            AnomalyScore = anomaly, ProductionIntensityPercent = production,
            RecordCount = 1
        };
    }
}
