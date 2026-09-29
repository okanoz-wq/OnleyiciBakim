using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Failures;
using OnleyiciBakim.Contracts.Machines;
using OnleyiciBakim.Contracts.Maintenance;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

/// <summary>
/// Read-only adapter for the centrally supplied BakimYonetimiDb schema.
/// </summary>
[ApiController]
[Route("api/v1/bakim-yonetimi")]
public sealed class BakimYonetimiDataController(
    BakimYonetimiDbContext dbContext,
    IRiskClassificationService classificationService,
    IReadModelMlConfidenceService mlConfidenceService) : ControllerBase
{
    [HttpGet("machines")]
    public async Task<ActionResult<PagedResponse<MachineListItemResponse>>> GetMachines(
        [FromQuery] MachineQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Makineler.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => (x.MakineAdi != null && x.MakineAdi.Contains(search)) ||
                                     (x.MakineKodu != null && x.MakineKodu.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(request.CompanyId))
            query = query.Where(x => x.FirmaId == request.CompanyId);
        if (!string.IsNullOrWhiteSpace(request.BranchId))
            query = query.Where(x => x.SubeId == request.BranchId);
        if (!string.IsNullOrWhiteSpace(request.DepartmentId))
            query = query.Where(x => x.DepartmanId == request.DepartmentId);
        if (!string.IsNullOrWhiteSpace(request.ProductionLineId))
            query = query.Where(x => x.HatId == request.ProductionLineId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Durum == request.Status);

        query = request.SortBy.Equals("name", StringComparison.OrdinalIgnoreCase)
            ? request.Descending ? query.OrderByDescending(x => x.MakineAdi) : query.OrderBy(x => x.MakineAdi)
            : request.SortBy.Equals("code", StringComparison.OrdinalIgnoreCase)
                ? request.Descending ? query.OrderByDescending(x => x.MakineKodu) : query.OrderBy(x => x.MakineKodu)
                : request.Descending ? query.OrderByDescending(x => x.GuncelRiskPuani) : query.OrderBy(x => x.GuncelRiskPuani);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.MakineId,
                x.MakineKodu,
                x.MakineAdi,
                x.Model,
                Location = x.Hat.HatAdi,
                x.Durum,
                x.SonBakimTarihi,
                x.GuncelRiskPuani,
                TotalFailureCount = x.ArizaKartlari.Count,
                NextMaintenanceAt = x.BakimPlanlari
                    .Where(plan => plan.Durum != "Tamamlandı" && plan.Durum != "İptal")
                    .OrderBy(plan => plan.PlanlananTarih)
                    .Select(plan => (DateTime?)plan.PlanlananTarih)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new MachineListItemResponse(
            x.MakineId,
            x.MakineKodu ?? x.MakineId,
            x.MakineAdi ?? x.MakineId,
            x.Model,
            x.Location,
            x.Durum ?? "Bilinmiyor",
            BakimYonetimiDashboardService.ToUtc(x.SonBakimTarihi),
            BakimYonetimiDashboardService.ToUtc(x.NextMaintenanceAt),
            x.TotalFailureCount,
            PresentRisk(x.GuncelRiskPuani ?? 0m))).ToArray();

        return Ok(new PagedResponse<MachineListItemResponse>(
            items, request.Page, request.PageSize, total));
    }

    [HttpGet("machines/{id}")]
    public async Task<ActionResult<MachineDetailResponse>> GetMachine(
        string id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.Makineler.AsNoTracking()
            .Where(x => x.MakineId == id)
            .Select(x => new
            {
                x.MakineId,
                x.MakineKodu,
                x.MakineAdi,
                x.Model,
                x.KritikSeviye,
                x.Durum,
                x.KurulumYili,
                Company = x.Firma.FirmaAdi,
                Branch = x.Sube.SubeAdi,
                Department = x.Departman.DepartmanAdi,
                ProductionLine = x.Hat.HatAdi,
                x.SonBakimTarihi,
                x.GuncelRiskPuani,
                TotalFailureCount = x.ArizaKartlari.Count,
                NextMaintenanceAt = x.BakimPlanlari
                    .Where(plan => plan.Durum != "Tamamlandı" && plan.Durum != "İptal")
                    .OrderBy(plan => plan.PlanlananTarih)
                    .Select(plan => (DateTime?)plan.PlanlananTarih)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");

        return Ok(new MachineDetailResponse(
            row.MakineId,
            row.MakineKodu ?? row.MakineId,
            row.MakineAdi ?? row.MakineId,
            row.Model,
            row.Model,
            row.KritikSeviye ?? "Orta",
            row.Durum ?? "Bilinmiyor",
            row.KurulumYili,
            row.Company,
            row.Branch,
            row.Department,
            row.ProductionLine,
            BakimYonetimiDashboardService.ToUtc(row.SonBakimTarihi),
            BakimYonetimiDashboardService.ToUtc(row.NextMaintenanceAt),
            row.TotalFailureCount,
            PresentRisk(row.GuncelRiskPuani ?? 0m),
            [],
            []));
    }

    [HttpGet("failures")]
    public async Task<ActionResult<PagedResponse<FailureResponse>>> GetFailures(
        [FromQuery] FailureQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ArizaKartlari.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MachineId))
            query = query.Where(x => x.MakineId == request.MachineId);
        if (!string.IsNullOrWhiteSpace(request.FailureType))
            query = query.Where(x => x.ArizaTuru == request.FailureType);
        if (!string.IsNullOrWhiteSpace(request.Severity))
            query = query.Where(x => x.OnemSeviyesi == request.Severity);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Durum == request.Status);
        if (request.From.HasValue)
        {
            var from = request.From.Value.UtcDateTime;
            query = query.Where(x => x.ArizaTarihi >= from);
        }
        if (request.To.HasValue)
        {
            var to = request.To.Value.UtcDateTime;
            query = query.Where(x => x.ArizaTarihi <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.ArizaTarihi)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.ArizaId,
                x.ArizaTarihi,
                x.MakineId,
                MachineName = x.Makine.MakineAdi,
                x.ArizaTuru,
                x.OnemSeviyesi,
                x.DurusSuresiDk,
                Technician = x.Personel != null ? x.Personel.AdSoyad : null,
                x.Durum,
                x.KokNeden,
                x.IsEmriNo,
                x.TahminiMaliyetTl
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new FailureResponse(
            x.ArizaId,
            BakimYonetimiDashboardService.ToUtc(x.ArizaTarihi),
            x.MakineId,
            x.MachineName ?? x.MakineId,
            x.ArizaTuru ?? "Belirtilmemiş",
            x.OnemSeviyesi ?? "Belirtilmemiş",
            Math.Round((x.DurusSuresiDk ?? 0) / 60m, 1),
            x.Technician,
            x.Durum ?? "Bilinmiyor",
            x.KokNeden,
            x.IsEmriNo,
            x.TahminiMaliyetTl,
            null)).ToArray();

        return Ok(new PagedResponse<FailureResponse>(items, request.Page, request.PageSize, total));
    }

    [HttpGet("maintenance/plans")]
    public async Task<ActionResult<PagedResponse<MaintenancePlanResponse>>> GetMaintenancePlans(
        [FromQuery] MaintenancePlanQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BakimPlanlari.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MachineId))
            query = query.Where(x => x.MakineId == request.MachineId);
        if (request.From.HasValue)
        {
            var from = request.From.Value.UtcDateTime;
            query = query.Where(x => x.PlanlananTarih >= from);
        }
        if (request.To.HasValue)
        {
            var to = request.To.Value.UtcDateTime;
            query = query.Where(x => x.PlanlananTarih <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.PlanlananTarih)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.PlanId,
                x.MakineId,
                MachineName = x.Makine.MakineAdi,
                x.PlanlananTarih,
                x.BakimTuru,
                x.Oncelik,
                x.Durum,
                x.Gerekce,
                MlProbability = x.Makine.GuncelRiskler
                    .OrderByDescending(risk => risk.DegerlendirmeTarihi)
                    .Select(risk => risk.MlOlasiligi)
                    .FirstOrDefault(),
                MlCalculationDate = x.Makine.GuncelRiskler
                    .OrderByDescending(risk => risk.DegerlendirmeTarihi)
                    .Select(risk => (DateTime?)risk.DegerlendirmeTarihi)
                    .FirstOrDefault(),
                RiskScore = dbContext.RiskAnalizleri
                    .Where(risk => risk.AnalizId == x.AnalizId)
                    .Select(risk => (decimal?)risk.NihaiPuan)
                    .FirstOrDefault() ?? x.Makine.GuncelRiskPuani ?? 0m,
                Responsible = x.SorumluPersonel != null ? x.SorumluPersonel.AdSoyad : null,
                WorkOrder = x.IsEmirleri.OrderByDescending(order => order.OlusturmaTarihi)
                    .Select(order => order.IsEmriNo).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var liveConfidences = await mlConfidenceService.GetConfidencesAsync(
            rows
                .Where(x => !x.MlProbability.HasValue)
                .Select(x => new ReadModelMlConfidenceRequest(
                    x.MakineId,
                    x.MlCalculationDate ?? x.PlanlananTarih))
                .ToArray(),
            cancellationToken);

        var items = rows.Select(x => new MaintenancePlanResponse(
            x.PlanId,
            x.MakineId,
            x.MachineName ?? x.MakineId,
            BakimYonetimiDashboardService.ToUtc(x.PlanlananTarih),
            x.BakimTuru ?? "Belirtilmemiş",
            x.Oncelik ?? "Orta",
            x.Durum ?? "Bilinmiyor",
            ToModelConfidence(x.MlProbability) ??
                liveConfidences.GetValueOrDefault(x.MakineId),
            x.RiskScore,
            x.Gerekce,
            x.Gerekce,
            x.Responsible,
            x.WorkOrder)).ToArray();

        return Ok(new PagedResponse<MaintenancePlanResponse>(
            items, request.Page, request.PageSize, total));
    }

    private RiskPresentation PresentRisk(decimal score)
    {
        var level = classificationService.Classify(score);
        return new RiskPresentation(
            score,
            classificationService.GetLabel(level),
            classificationService.GetColorName(level),
            classificationService.GetColorHex(level));
    }

    private static decimal? ToModelConfidence(decimal? failureProbability)
    {
        if (!failureProbability.HasValue)
            return null;

        var probabilityPercent = failureProbability.Value is >= 0m and <= 1m
            ? failureProbability.Value * 100m
            : failureProbability.Value;
        probabilityPercent = Math.Clamp(probabilityPercent, 0m, 100m);
        return Math.Round(Math.Max(probabilityPercent, 100m - probabilityPercent), 1);
    }
}
