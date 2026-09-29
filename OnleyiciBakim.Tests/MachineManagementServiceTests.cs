using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services;
using OnleyiciBakim.Services.Management;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class MachineManagementServiceTests
{
    [Fact]
    public async Task Create_WritesLocalMachineAgainstValidatedHierarchy()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(ValidRequest("LOCAL-01"), CancellationToken.None);

        var entity = await db.Makineler.SingleAsync();
        Assert.True(result.Success);
        Assert.Equal("Local", entity.VeriKaynagi);
        Assert.Equal("LOCAL-01", entity.MakineKodu);
    }

    [Fact]
    public async Task Create_ImmediatelyRequestsInitialRiskCalculation()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        var risk = new RecordingRiskAssessmentService();
        var service = CreateService(db, risk: risk);

        var result = await service.CreateAsync(ValidRequest("LOCAL-RISK"), CancellationToken.None);

        Assert.Equal(result.Id, Assert.Single(risk.MachineIds));
    }

    [Fact]
    public async Task Create_RejectsDuplicateMachineCode()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        db.Makineler.Add(Machine("M-1", "DUPLICATE", "Local"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(ValidRequest("DUPLICATE"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_RejectsErpOwnedMachine()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        db.Makineler.Add(Machine("ERP-1", "ERP-01", "ERP"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync("ERP-1", ValidRequest("ERP-01"), CancellationToken.None));
    }

    [Fact]
    public async Task Deactivate_UsesSoftDeleteAndKeepsMachine()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        db.Makineler.Add(Machine("LOCAL-1", "LOCAL-01", "Local"));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.DeactivateAsync("LOCAL-1", CancellationToken.None);

        Assert.True(result.Deactivated);
        Assert.Equal("Pasif", (await db.Makineler.SingleAsync()).Durum);
    }

    [Fact]
    public async Task Create_IsBlockedWhenErpIsMasterSource()
    {
        await using var db = CreateContext();
        SeedHierarchy(db);
        var service = CreateService(db, master: true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(ValidRequest("LOCAL-01"), CancellationToken.None));
    }

    private static MachineManagementService CreateService(
        BakimYonetimiDbContext db,
        bool master = false,
        ILocalRiskAssessmentService? risk = null) =>
        new(db, new RiskClassificationService(),
            Microsoft.Extensions.Options.Options.Create(
                new ErpIntegrationOptions { Enabled = master, UseErpAsMasterSource = master }),
            NullLogger<MachineManagementService>.Instance,
            risk);

    private sealed class RecordingRiskAssessmentService : ILocalRiskAssessmentService
    {
        public List<string> MachineIds { get; } = [];

        public Task<RiskAssessmentResponse> EvaluateMachineAsync(
            string machineId,
            string calculationReason,
            CancellationToken cancellationToken)
        {
            MachineIds.Add(machineId);
            return Task.FromResult<RiskAssessmentResponse>(null!);
        }
    }

    private static MachineUpsertRequest ValidRequest(string code) => new()
    {
        Code = code, Name = "Test Makinesi", CompanyId = "F-1", BranchId = "S-1",
        DepartmentId = "D-1", ProductionLineId = "H-1", Age = 2,
        InstallationYear = DateTime.Today.Year - 2, Criticality = "Orta", Status = "Aktif"
    };

    private static void SeedHierarchy(BakimYonetimiDbContext db)
    {
        var company = new Firmalar { FirmaId = "F-1", FirmaKodu = "F", FirmaAdi = "Firma", AktifMi = true };
        var branch = new Subeler { SubeId = "S-1", FirmaId = company.FirmaId, SubeKodu = "S", SubeAdi = "Şube", AktifMi = true, Firma = company };
        var department = new Departmanlar { DepartmanId = "D-1", SubeId = branch.SubeId, DepartmanKodu = "D", DepartmanAdi = "Departman", AktifMi = true, Sube = branch };
        var line = new UretimHatlari { HatId = "H-1", DepartmanId = department.DepartmanId, HatKodu = "H", HatAdi = "Hat", VardiyaSayisi = 3, AktifMi = true, Departman = department };
        db.AddRange(company, branch, department, line);
        db.SaveChanges();
    }

    private static Makineler Machine(string id, string code, string source) => new()
    {
        MakineId = id, KaynakMakineId = id.GetHashCode(), FirmaId = "F-1", SubeId = "S-1",
        DepartmanId = "D-1", HatId = "H-1", MakineKodu = code, MakineAdi = "Makine",
        Yas = 2, KurulumYili = DateTime.Today.Year - 2, KritikSeviye = "Orta",
        Durum = "Aktif", SonBakimTarihi = DateTime.Today.AddMonths(-1), VeriKaynagi = source
    };

    private static BakimYonetimiDbContext CreateContext() => new(
        new DbContextOptionsBuilder<BakimYonetimiDbContext>()
            .UseInMemoryDatabase($"machine-management-{Guid.NewGuid():N}").Options);
}
