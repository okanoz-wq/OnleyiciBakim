using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Machines;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/machines")]
public sealed class MachinesController(
    ApplicationDbContext dbContext,
    IRiskClassificationService classificationService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<MachineListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<MachineListItemResponse>>> GetAll(
        [FromQuery] MachineQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Machines.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{request.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) ||
                                     EF.Functions.ILike(x.Code, pattern));
        }
        if (!string.IsNullOrWhiteSpace(request.CompanyId))
            query = query.Where(x => x.CompanyId == request.CompanyId);
        if (!string.IsNullOrWhiteSpace(request.BranchId))
            query = query.Where(x => x.BranchId == request.BranchId);
        if (!string.IsNullOrWhiteSpace(request.DepartmentId))
            query = query.Where(x => x.DepartmentId == request.DepartmentId);
        if (!string.IsNullOrWhiteSpace(request.ProductionLineId))
            query = query.Where(x => x.ProductionLineId == request.ProductionLineId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Status == request.Status);
        if (TryParseRiskLevel(request.RiskLevel, out var riskLevel))
            query = query.Where(x => x.CurrentRiskLevel == riskLevel);

        query = (request.SortBy.ToLowerInvariant(), request.Descending) switch
        {
            ("name", false) => query.OrderBy(x => x.Name),
            ("name", true) => query.OrderByDescending(x => x.Name),
            ("code", false) => query.OrderBy(x => x.Code),
            ("code", true) => query.OrderByDescending(x => x.Code),
            ("riskscore", false) => query.OrderBy(x => x.CurrentRiskScore),
            _ => query.OrderByDescending(x => x.CurrentRiskScore)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                Location = x.ProductionLine != null ? x.ProductionLine.Name : null,
                x.Status,
                x.LastMaintenanceAt,
                x.CurrentRiskScore,
                x.CurrentRiskLevel,
                TotalFailureCount = x.Failures.Count,
                NextMaintenanceAt = x.MaintenancePlans
                    .Where(plan => plan.Status == MaintenancePlanStatus.Planned ||
                                   plan.Status == MaintenancePlanStatus.InProgress)
                    .OrderBy(plan => plan.PlannedAt)
                    .Select(plan => (DateTimeOffset?)plan.PlannedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new MachineListItemResponse(
            x.Id,
            x.Code,
            x.Name,
            x.Type,
            x.Location,
            x.Status,
            x.LastMaintenanceAt,
            x.NextMaintenanceAt,
            x.TotalFailureCount,
            PresentRisk(x.CurrentRiskScore, x.CurrentRiskLevel))).ToArray();

        return Ok(new PagedResponse<MachineListItemResponse>(
            items, request.Page, request.PageSize, totalCount));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<MachineDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MachineDetailResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.Machines
            .AsNoTracking()
            .Include(x => x.Company)
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.ProductionLine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");

        var recentFailures = await dbContext.FailureRecords
            .AsNoTracking()
            .Where(x => x.MachineId == id)
            .OrderByDescending(x => x.OccurredAt)
            .Take(10)
            .Select(x => new MachineFailureSummary(
                x.Id,
                x.OccurredAt,
                x.FailureType,
                x.Severity,
                Math.Round((x.DowntimeMinutes ?? 0) / 60m, 1),
                x.Personnel != null ? x.Personnel.FullName : null,
                x.Status.ToString()))
            .ToListAsync(cancellationToken);

        var telemetry = await dbContext.DailyTelemetry
            .AsNoTracking()
            .Where(x => x.MachineId == id)
            .OrderByDescending(x => x.Date)
            .Take(30)
            .Select(x => new TelemetryResponse(
                x.Date,
                x.VoltageAverage,
                x.RotationAverage,
                x.PressureAverage,
                x.VibrationAverage,
                x.AnomalyScore,
                x.ProductionIntensityPercent))
            .ToListAsync(cancellationToken);

        var nextMaintenance = await dbContext.MaintenancePlans
            .AsNoTracking()
            .Where(x => x.MachineId == id &&
                        (x.Status == MaintenancePlanStatus.Planned ||
                         x.Status == MaintenancePlanStatus.InProgress))
            .OrderBy(x => x.PlannedAt)
            .Select(x => (DateTimeOffset?)x.PlannedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var failureCount = await dbContext.FailureRecords.CountAsync(x => x.MachineId == id, cancellationToken);

        return Ok(new MachineDetailResponse(
            machine.Id,
            machine.Code,
            machine.Name,
            machine.Type,
            machine.Model,
            machine.Criticality,
            machine.Status,
            machine.InstallationYear,
            machine.Company?.Name,
            machine.Branch?.Name,
            machine.Department?.Name,
            machine.ProductionLine?.Name,
            machine.LastMaintenanceAt,
            nextMaintenance,
            failureCount,
            PresentRisk(machine.CurrentRiskScore, machine.CurrentRiskLevel),
            recentFailures,
            telemetry));
    }

    [HttpPost]
    [ProducesResponseType<MachineDetailResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateMachineRequest request,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Machines.AnyAsync(
                x => x.Id == request.Id || x.Code == request.Code,
                cancellationToken))
        {
            throw new ConflictException("Aynı makine kimliği veya kodu zaten kullanılıyor.");
        }

        var machine = new Machine
        {
            Id = request.Id,
            ExternalId = request.ExternalId,
            CompanyId = request.CompanyId,
            BranchId = request.BranchId,
            DepartmentId = request.DepartmentId,
            ProductionLineId = request.ProductionLineId,
            Code = request.Code,
            Name = request.Name,
            Type = request.Type,
            Model = request.Model,
            Age = request.Age,
            Criticality = request.Criticality,
            InstallationYear = request.InstallationYear,
            Status = request.Status,
            DataSource = request.DataSource
        };
        dbContext.Machines.Add(machine);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = machine.Id }, new { machine.Id });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        string id,
        UpdateMachineRequest request,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");
        if (await dbContext.Machines.AnyAsync(x => x.Id != id && x.Code == request.Code, cancellationToken))
            throw new ConflictException($"'{request.Code}' makine kodu zaten kullanılıyor.");

        machine.Code = request.Code;
        machine.Name = request.Name;
        machine.CompanyId = request.CompanyId;
        machine.BranchId = request.BranchId;
        machine.DepartmentId = request.DepartmentId;
        machine.ProductionLineId = request.ProductionLineId;
        machine.Type = request.Type;
        machine.Model = request.Model;
        machine.Age = request.Age;
        machine.Criticality = request.Criticality;
        machine.InstallationYear = request.InstallationYear;
        machine.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        var machine = await dbContext.Machines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");
        machine.Status = "Pasif";
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/telemetry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertTelemetry(
        string id,
        TelemetryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Machines.AnyAsync(x => x.Id == id, cancellationToken))
            throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");

        var telemetry = await dbContext.DailyTelemetry
            .FirstOrDefaultAsync(x => x.MachineId == id && x.Date == request.Date, cancellationToken);
        if (telemetry is null)
        {
            telemetry = new DailyTelemetry { MachineId = id, Date = request.Date };
            dbContext.DailyTelemetry.Add(telemetry);
        }
        telemetry.VoltageAverage = request.VoltageAverage;
        telemetry.VoltageStdDev = request.VoltageStdDev;
        telemetry.RotationAverage = request.RotationAverage;
        telemetry.RotationStdDev = request.RotationStdDev;
        telemetry.PressureAverage = request.PressureAverage;
        telemetry.PressureStdDev = request.PressureStdDev;
        telemetry.VibrationAverage = request.VibrationAverage;
        telemetry.VibrationStdDev = request.VibrationStdDev;
        telemetry.RecordCount = request.RecordCount;
        telemetry.AnomalyScore = request.AnomalyScore;
        telemetry.ProductionIntensityPercent = request.ProductionIntensityPercent;
        telemetry.DataSource = request.DataSource;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private RiskPresentation PresentRisk(decimal score, RiskLevel level) =>
        new(score, classificationService.GetLabel(level), classificationService.GetColorName(level),
            classificationService.GetColorHex(level));

    private static bool TryParseRiskLevel(string? value, out RiskLevel level)
    {
        level = RiskLevel.Low;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim().ToUpperInvariant();
        level = normalized switch
        {
            "DÜŞÜK" or "DUSUK" or "LOW" => RiskLevel.Low,
            "ORTA" or "MEDIUM" => RiskLevel.Medium,
            "YÜKSEK" or "YUKSEK" or "HIGH" => RiskLevel.High,
            "KRİTİK" or "KRITIK" or "CRITICAL" => RiskLevel.Critical,
            _ => level
        };
        return normalized is "DÜŞÜK" or "DUSUK" or "LOW" or "ORTA" or "MEDIUM" or
            "YÜKSEK" or "YUKSEK" or "HIGH" or "KRİTİK" or "KRITIK" or "CRITICAL";
    }
}
