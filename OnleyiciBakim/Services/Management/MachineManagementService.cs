using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Options;

namespace OnleyiciBakim.Services.Management;

public interface IMachineManagementService
{
    Task<PagedResponse<ManagedMachineListItem>> GetAsync(
        ManagedMachineQuery query, CancellationToken cancellationToken);
    Task<ManagedMachineDetails> GetDetailsAsync(string id, CancellationToken cancellationToken);
    Task<MachineManagementLookups> GetLookupsAsync(CancellationToken cancellationToken);
    Task<ManagementOperationResult> CreateAsync(
        MachineUpsertRequest request, CancellationToken cancellationToken);
    Task<ManagementOperationResult> UpdateAsync(
        string id, MachineUpsertRequest request, CancellationToken cancellationToken);
    Task<ManagementOperationResult> DeactivateAsync(string id, CancellationToken cancellationToken);
    Task<ManagementOperationResult> ActivateAsync(string id, CancellationToken cancellationToken);
}

public sealed class MachineManagementService(
    BakimYonetimiDbContext dbContext,
    IRiskClassificationService riskClassificationService,
    IOptions<ErpIntegrationOptions> erpOptions,
    ILogger<MachineManagementService> logger,
    ILocalRiskAssessmentService? riskAssessmentService = null) : IMachineManagementService
{
    public async Task<PagedResponse<ManagedMachineListItem>> GetAsync(
        ManagedMachineQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Makineler.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                (x.MakineKodu != null && x.MakineKodu.Contains(search)) ||
                (x.MakineAdi != null && x.MakineAdi.Contains(search)) ||
                (x.Model != null && x.Model.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Durum == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Criticality))
            query = query.Where(x => x.KritikSeviye == request.Criticality);
        if (!string.IsNullOrWhiteSpace(request.SourceSystem))
            query = query.Where(x => x.VeriKaynagi == request.SourceSystem);

        query = (request.SortBy.Trim().ToLowerInvariant(), request.Descending) switch
        {
            ("code", false) => query.OrderBy(x => x.MakineKodu),
            ("code", true) => query.OrderByDescending(x => x.MakineKodu),
            ("name", false) => query.OrderBy(x => x.MakineAdi),
            ("name", true) => query.OrderByDescending(x => x.MakineAdi),
            ("status", false) => query.OrderBy(x => x.Durum),
            ("status", true) => query.OrderByDescending(x => x.Durum),
            ("risk", false) => query.OrderBy(x => x.GuncelRiskPuani),
            _ => query.OrderByDescending(x => x.GuncelRiskPuani)
        };

        var total = await query.CountAsync(cancellationToken);
        var masterSource = erpOptions.Value.UseErpAsMasterSource;
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
                x.KritikSeviye,
                x.KurulumYili,
                x.SonBakimTarihi,
                x.GuncelRiskPuani,
                x.VeriKaynagi,
                RelatedRecordCount = x.MakineBilesenler.Count + x.ArizaKartlari.Count +
                    x.BakimKayitlari.Count + x.BakimPlanlari.Count + x.GuncelRiskler.Count
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x =>
        {
            var level = riskClassificationService.Classify(x.GuncelRiskPuani ?? 0m);
            var source = NormalizeSource(x.VeriKaynagi);
            return new ManagedMachineListItem(
                x.MakineId,
                x.MakineKodu ?? x.MakineId,
                x.MakineAdi ?? x.MakineId,
                x.Model,
                x.Location ?? "Belirtilmemiş",
                x.Durum ?? "Bilinmiyor",
                x.KritikSeviye ?? "Orta",
                x.KurulumYili,
                ToUtc(x.SonBakimTarihi),
                x.GuncelRiskPuani ?? 0m,
                riskClassificationService.GetLabel(level),
                riskClassificationService.GetColorName(level),
                source,
                CanManage(source, masterSource),
                x.RelatedRecordCount);
        }).ToArray();

        return new PagedResponse<ManagedMachineListItem>(
            items, request.Page, request.PageSize, total);
    }

    public async Task<ManagedMachineDetails> GetDetailsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.Makineler.AsNoTracking()
            .Where(x => x.MakineId == id)
            .Select(x => new
            {
                Machine = x,
                Company = x.Firma.FirmaAdi,
                Branch = x.Sube.SubeAdi,
                Department = x.Departman.DepartmanAdi,
                ProductionLine = x.Hat.HatAdi,
                ComponentCount = x.MakineBilesenler.Count,
                FailureCount = x.ArizaKartlari.Count,
                MaintenanceCount = x.BakimKayitlari.Count,
                TelemetryCount = x.TelemetriGunluk.Count,
                RiskAnalysisCount = x.RiskAnalizleri.Count
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");

        var components = await dbContext.MakineBilesenler.AsNoTracking()
            .Where(x => x.MakineId == id)
            .OrderBy(x => x.BilesenKodu)
            .Select(x => new ManagedMachineComponentSummary(
                x.BilesenId, x.BilesenKodu, x.BilesenAdi, x.BilesenTuru, x.Kritiklik, x.Aktif))
            .ToListAsync(cancellationToken);
        var failureRows = await dbContext.ArizaKartlari.AsNoTracking()
            .Where(x => x.MakineId == id)
            .OrderByDescending(x => x.ArizaTarihi)
            .Take(10)
            .Select(x => new { x.ArizaId, x.ArizaTarihi, x.ArizaTuru, x.Durum, x.Aciklama })
            .ToListAsync(cancellationToken);
        var failures = failureRows.Select(x => new ManagedMachineHistorySummary(
            x.ArizaId, ToUtc(x.ArizaTarihi), x.ArizaTuru ?? "Arıza",
            x.Durum ?? "Bilinmiyor", x.Aciklama)).ToArray();
        var maintenanceRows = await dbContext.BakimKayitlari.AsNoTracking()
            .Where(x => x.MakineId == id)
            .OrderByDescending(x => x.BakimTarihi)
            .Take(10)
            .Select(x => new { x.BakimId, x.BakimTarihi, x.BakimTuru, x.Durum, x.Sonuc })
            .ToListAsync(cancellationToken);
        var maintenances = maintenanceRows.Select(x => new ManagedMachineHistorySummary(
            x.BakimId, ToUtc(x.BakimTarihi), x.BakimTuru ?? "Bakım",
            x.Durum ?? "Bilinmiyor", x.Sonuc)).ToArray();
        var telemetryRows = await dbContext.TelemetriGunluk.AsNoTracking()
            .Where(x => x.MakineId == id)
            .OrderByDescending(x => x.Tarih)
            .Take(14)
            .Select(x => new { x.Tarih, x.AnomaliPuani, x.UretimYogunluguYuzde, x.TitresimOrt })
            .ToListAsync(cancellationToken);
        var telemetry = telemetryRows.Select(x => new ManagedMachineTelemetrySummary(
            ToUtc(x.Tarih), x.AnomaliPuani, x.UretimYogunluguYuzde, x.TitresimOrt)).ToArray();

        var machine = row.Machine;
        var source = NormalizeSource(machine.VeriKaynagi);
        var level = riskClassificationService.Classify(machine.GuncelRiskPuani ?? 0m);
        var relatedCount = row.ComponentCount + row.FailureCount + row.MaintenanceCount +
                           row.TelemetryCount + row.RiskAnalysisCount;
        return new ManagedMachineDetails(
            machine.MakineId,
            machine.MakineKodu ?? machine.MakineId,
            machine.MakineAdi ?? machine.MakineId,
            machine.Model,
            machine.Yas,
            machine.KurulumYili,
            machine.KritikSeviye ?? "Orta",
            machine.Durum ?? "Bilinmiyor",
            ToUtc(machine.SonBakimTarihi),
            machine.FirmaId,
            row.Company ?? machine.FirmaId,
            machine.SubeId,
            row.Branch ?? machine.SubeId,
            machine.DepartmanId,
            row.Department ?? machine.DepartmanId,
            machine.HatId,
            row.ProductionLine ?? machine.HatId,
            machine.GuncelRiskPuani ?? 0m,
            riskClassificationService.GetLabel(level),
            source,
            machine.KaynakMakineId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CanManage(source, erpOptions.Value.UseErpAsMasterSource),
            relatedCount > 0,
            row.ComponentCount,
            row.FailureCount,
            row.MaintenanceCount,
            row.TelemetryCount,
            row.RiskAnalysisCount,
            components,
            failures,
            maintenances,
            telemetry);
    }

    public async Task<MachineManagementLookups> GetLookupsAsync(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Firmalar.AsNoTracking().Where(x => x.AktifMi)
            .OrderBy(x => x.FirmaAdi)
            .Select(x => new ManagementLookupItem(x.FirmaId, x.FirmaKodu ?? x.FirmaId,
                x.FirmaAdi ?? x.FirmaId, null))
            .ToListAsync(cancellationToken);
        var branches = await dbContext.Subeler.AsNoTracking().Where(x => x.AktifMi)
            .OrderBy(x => x.SubeAdi)
            .Select(x => new ManagementLookupItem(x.SubeId, x.SubeKodu ?? x.SubeId,
                x.SubeAdi ?? x.SubeId, x.FirmaId))
            .ToListAsync(cancellationToken);
        var departments = await dbContext.Departmanlar.AsNoTracking().Where(x => x.AktifMi)
            .OrderBy(x => x.DepartmanAdi)
            .Select(x => new ManagementLookupItem(x.DepartmanId, x.DepartmanKodu ?? x.DepartmanId,
                x.DepartmanAdi ?? x.DepartmanId, x.SubeId))
            .ToListAsync(cancellationToken);
        var productionLines = await dbContext.UretimHatlari.AsNoTracking().Where(x => x.AktifMi)
            .OrderBy(x => x.HatAdi)
            .Select(x => new ManagementLookupItem(x.HatId, x.HatKodu ?? x.HatId,
                x.HatAdi ?? x.HatId, x.DepartmanId))
            .ToListAsync(cancellationToken);
        return new MachineManagementLookups(
            companies, branches, departments, productionLines,
            MachineManagementRules.Criticalities,
            MachineManagementRules.Statuses,
            erpOptions.Value.UseErpAsMasterSource);
    }

    public async Task<ManagementOperationResult> CreateAsync(
        MachineUpsertRequest request,
        CancellationToken cancellationToken)
    {
        EnsureLocalCreateAllowed();
        await ValidateRequestAsync(null, request, cancellationToken);
        var machineId = await NewMachineIdAsync(cancellationToken);
        var sourceId = (await dbContext.Makineler.MaxAsync(
            x => (int?)x.KaynakMakineId, cancellationToken) ?? 0) + 1;
        var lastMaintenance = request.LastMaintenanceDate?.Date ??
            new DateTime(request.InstallationYear, 1, 1);
        dbContext.Makineler.Add(new Makineler
        {
            MakineId = machineId,
            KaynakMakineId = sourceId,
            FirmaId = request.CompanyId,
            SubeId = request.BranchId,
            DepartmanId = request.DepartmentId,
            HatId = request.ProductionLineId,
            MakineKodu = request.Code.Trim(),
            MakineAdi = request.Name.Trim(),
            Model = NullIfWhiteSpace(request.Model),
            Yas = request.Age,
            KritikSeviye = request.Criticality,
            KurulumYili = request.InstallationYear,
            Durum = request.Status,
            SonBakimTarihi = lastMaintenance,
            GuncelRiskPuani = 0m,
            GuncelRiskSeviyesi = "Düşük",
            VeriKaynagi = "Local"
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateRiskAsync(machineId, "Yeni makine oluşturuldu", cancellationToken);
        logger.LogInformation(
            "Entity=Machine Id={MachineId} Operation=Create Success=true At={At}",
            machineId, DateTimeOffset.UtcNow);
        return new ManagementOperationResult(
            true, "Makine başarıyla oluşturuldu.", machineId);
    }

    public async Task<ManagementOperationResult> UpdateAsync(
        string id,
        MachineUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var machine = await GetMutableMachineAsync(id, cancellationToken);
        await ValidateRequestAsync(id, request, cancellationToken);
        machine.FirmaId = request.CompanyId;
        machine.SubeId = request.BranchId;
        machine.DepartmanId = request.DepartmentId;
        machine.HatId = request.ProductionLineId;
        machine.MakineKodu = request.Code.Trim();
        machine.MakineAdi = request.Name.Trim();
        machine.Model = NullIfWhiteSpace(request.Model);
        machine.Yas = request.Age;
        machine.KritikSeviye = request.Criticality;
        machine.KurulumYili = request.InstallationYear;
        machine.Durum = request.Status;
        if (request.LastMaintenanceDate.HasValue)
            machine.SonBakimTarihi = request.LastMaintenanceDate.Value.Date;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateRiskAsync(id, "Makine bilgileri güncellendi", cancellationToken);
        logger.LogInformation(
            "Entity=Machine Id={MachineId} Operation=Update Success=true At={At}",
            id, DateTimeOffset.UtcNow);
        return new ManagementOperationResult(true, "Makine başarıyla güncellendi.", id);
    }

    public async Task<ManagementOperationResult> DeactivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var machine = await GetMutableMachineAsync(id, cancellationToken);
        machine.Durum = "Pasif";
        await dbContext.SaveChangesAsync(cancellationToken);
        var hasRelations = await HasRelationsAsync(id, cancellationToken);
        logger.LogInformation(
            "Entity=Machine Id={MachineId} Operation=Deactivate Success=true At={At}",
            id, DateTimeOffset.UtcNow);
        return new ManagementOperationResult(
            true,
            hasRelations
                ? "Makine ilişkili kayıtları korumak için pasife alındı."
                : "Makine pasife alındı.",
            id,
            true);
    }

    public async Task<ManagementOperationResult> ActivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var machine = await GetMutableMachineAsync(id, cancellationToken);
        machine.Durum = "Aktif";
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateRiskAsync(id, "Makine yeniden aktifleştirildi", cancellationToken);
        logger.LogInformation(
            "Entity=Machine Id={MachineId} Operation=Activate Success=true At={At}",
            id, DateTimeOffset.UtcNow);
        return new ManagementOperationResult(true, "Makine aktifleştirildi.", id);
    }

    private async Task ValidateRequestAsync(
        string? currentId,
        MachineUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
            throw new DomainValidationException(validationResults[0].ErrorMessage ?? "Makine bilgileri geçersizdir.");
        if (request.InstallationYear > DateTime.UtcNow.Year)
            throw new DomainValidationException("Kurulum tarihi gelecek bir tarih olamaz.");
        if (!MachineManagementRules.Criticalities.Contains(request.Criticality))
            throw new DomainValidationException("Kritiklik seviyesi geçersizdir.");
        if (!MachineManagementRules.Statuses.Contains(request.Status))
            throw new DomainValidationException("Makine durumu geçersizdir.");
        if (await dbContext.Makineler.AsNoTracking().AnyAsync(
                x => x.MakineId != currentId && x.MakineKodu == request.Code.Trim(),
                cancellationToken))
            throw new ConflictException("Bu makine kodu zaten kullanılmaktadır.");
        var hierarchyValid = await dbContext.UretimHatlari.AsNoTracking().AnyAsync(
            line => line.HatId == request.ProductionLineId && line.DepartmanId == request.DepartmentId &&
                    line.AktifMi && line.Departman.AktifMi && line.Departman.Sube.AktifMi &&
                    line.Departman.Sube.Firma.AktifMi &&
                    line.Departman.SubeId == request.BranchId &&
                    line.Departman.Sube.FirmaId == request.CompanyId,
            cancellationToken);
        if (!hierarchyValid)
            throw new DomainValidationException(
                "Firma, şube, departman ve üretim hattı seçimi birbiriyle uyumlu değildir.");
    }

    private async Task RecalculateRiskAsync(
        string machineId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (riskAssessmentService is null)
            return;
        try
        {
            await riskAssessmentService.EvaluateMachineAsync(machineId, reason, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Ana veri kaydı başarılıysa geçici ML/analiz sorunu makine kaydını
            // başarısız göstermesin; worker sonraki turda yeniden hesaplar.
            logger.LogError(exception,
                "Makine kaydedildi ancak ilk risk hesabı tamamlanamadı. MachineId={MachineId}",
                machineId);
        }
    }

    private async Task<Makineler> GetMutableMachineAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var machine = await dbContext.Makineler.FirstOrDefaultAsync(
            x => x.MakineId == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli makine bulunamadı.");
        var source = NormalizeSource(machine.VeriKaynagi);
        if (!CanManage(source, erpOptions.Value.UseErpAsMasterSource))
            throw new ConflictException(
                "ERP tarafından yönetilen makine kullanıcı tarafından değiştirilemez veya silinemez.");
        return machine;
    }

    private void EnsureLocalCreateAllowed()
    {
        if (erpOptions.Value.UseErpAsMasterSource)
            throw new ConflictException(
                "ERP ana veri kaynağı modunda yerel makine oluşturulamaz.");
    }

    private async Task<bool> HasRelationsAsync(string id, CancellationToken cancellationToken) =>
        await dbContext.MakineBilesenler.AnyAsync(x => x.MakineId == id, cancellationToken) ||
        await dbContext.ArizaKartlari.AnyAsync(x => x.MakineId == id, cancellationToken) ||
        await dbContext.BakimKayitlari.AnyAsync(x => x.MakineId == id, cancellationToken) ||
        await dbContext.BakimPlanlari.AnyAsync(x => x.MakineId == id, cancellationToken) ||
        await dbContext.GuncelRiskler.AnyAsync(x => x.MakineId == id, cancellationToken);

    private async Task<string> NewMachineIdAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var id = ("LCL-" + Guid.NewGuid().ToString("N"))[..16].ToUpperInvariant();
            if (!await dbContext.Makineler.AnyAsync(x => x.MakineId == id, cancellationToken))
                return id;
        }
        throw new InvalidOperationException("Benzersiz makine kimliği üretilemedi.");
    }

    private static string NormalizeSource(string? source) =>
        string.Equals(source, "ERP", StringComparison.OrdinalIgnoreCase) ? "ERP" : "Local";

    private static bool CanManage(string source, bool masterSource) =>
        !masterSource && !string.Equals(source, "ERP", StringComparison.OrdinalIgnoreCase);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? ToUtc(DateTime? value) =>
        value.HasValue ? ToUtc(value.Value) : null;
}
