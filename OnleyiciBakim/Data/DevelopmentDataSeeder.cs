using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Data;

public sealed class DevelopmentDataSeeder(
    ApplicationDbContext dbContext,
    ILogger<DevelopmentDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Machines.AnyAsync(
                x => x.Id.StartsWith("DEV-"), cancellationToken))
            return;

        var company = new Company { Id = "DEV-COMP", Code = "DEV", Name = "Demo Üretim A.Ş." };
        var branch = new Branch
        {
            Id = "DEV-BRANCH", CompanyId = company.Id, Code = "IST", Name = "İstanbul Fabrika"
        };
        var department = new Department
        {
            Id = "DEV-DEPT", BranchId = branch.Id, Code = "URETIM", Name = "Üretim"
        };
        var line = new ProductionLine
        {
            Id = "DEV-LINE", DepartmentId = department.Id, Code = "HAT-1",
            Name = "Üretim Hattı 1", ShiftCount = 3
        };
        var personnel = new Personnel
        {
            Id = "DEV-PER-1", FullName = "Demo Teknisyen", Role = "Technician",
            Specialization = "Mekanik"
        };
        dbContext.AddRange(company, branch, department, line, personnel);

        var now = DateTimeOffset.UtcNow;
        var machines = new[]
        {
            Machine("DEV-MKN-001", "MKN-001", "CNC Torna 1", "CNC Torna", "Kritik", 88m, RiskLevel.Critical, 210m),
            Machine("DEV-MKN-002", "MKN-002", "Hidrolik Pres 2", "Hidrolik Pres", "Yüksek", 68m, RiskLevel.High, 95m),
            Machine("DEV-MKN-003", "MKN-003", "Konveyör Bant 3", "Konveyör", "Orta", 44m, RiskLevel.Medium, 35m),
            Machine("DEV-MKN-004", "MKN-004", "Paketleme Ünitesi 4", "Paketleme", "Düşük", 18m, RiskLevel.Low, 12m)
        };
        dbContext.Machines.AddRange(machines);
        dbContext.FailureRecords.AddRange(
            new FailureRecord
            {
                Id = "DEV-FLR-001", MachineId = machines[0].Id,
                OccurredAt = now.AddDays(-4), FailureType = "Mekanik", Severity = "Kritik",
                IsRecurring = true, DowntimeMinutes = 420, Status = RecordStatus.Open,
                Description = "Mil titreşimi sınır üzerinde.", DataSource = "DevelopmentSeed"
            },
            new FailureRecord
            {
                Id = "DEV-FLR-002", MachineId = machines[1].Id,
                OccurredAt = now.AddDays(-18), FailureType = "Hidrolik", Severity = "Yüksek",
                IsRecurring = false, DowntimeMinutes = 150, Status = RecordStatus.Closed,
                ClosedAt = now.AddDays(-17), Resolution = "Hidrolik hortum değiştirildi.",
                DataSource = "DevelopmentSeed"
            });
        dbContext.MaintenancePlans.AddRange(
            new MaintenancePlan
            {
                Id = "DEV-PLN-001", MachineId = machines[0].Id, PlannedAt = now.AddDays(2),
                MaintenanceType = "Acil", Priority = "Kritik", Status = MaintenancePlanStatus.Planned,
                Recommendation = "Mil ve rulman grubunu kontrol et.", DataSource = "DevelopmentSeed"
            },
            new MaintenancePlan
            {
                Id = "DEV-PLN-002", MachineId = machines[2].Id, PlannedAt = now.AddDays(20),
                MaintenanceType = "Periyodik", Priority = "Orta", Status = MaintenancePlanStatus.Draft,
                Recommendation = "Kayış gerginliği ve yağlama kontrolü.", DataSource = "DevelopmentSeed"
            });
        dbContext.DailyTelemetry.AddRange(machines.Select((machine, index) => new DailyTelemetry
        {
            MachineId = machine.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            RecordCount = 1440,
            AnomalyScore = Math.Max(5m, machine.CurrentRiskScore - 10m),
            ProductionIntensityPercent = 65m + index * 5m,
            DataSource = "DevelopmentSeed"
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development seed verileri oluşturuldu.");

        Machine Machine(
            string id, string code, string name, string type, string criticality,
            decimal risk, RiskLevel level, decimal workingHours) =>
            new()
            {
                Id = id, Code = code, Name = name, Type = type,
                Criticality = criticality, CurrentRiskScore = risk, CurrentRiskLevel = level,
                Status = "Aktif", CompanyId = company.Id, BranchId = branch.Id,
                DepartmentId = department.Id, ProductionLineId = line.Id,
                LastMaintenanceAt = now.AddDays(-(int)(risk + 10m)),
                WorkingHours = workingHours, CounterLimitHours = 180m,
                InstallationYear = 2021, DataSource = "DevelopmentSeed"
            };
    }
}
