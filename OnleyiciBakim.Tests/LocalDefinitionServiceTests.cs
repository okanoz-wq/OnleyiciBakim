using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services.Management;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class LocalDefinitionServiceTests
{
    [Fact]
    public async Task CompanyCode_IsUnique_AndDeactivateKeepsPhysicalRecord()
    {
        await using var db = Context(); var service = new LocalDefinitionService(db);
        var row = await service.SaveAsync("companies", null, Request("F01", "Firma"), CancellationToken.None);
        await Assert.ThrowsAsync<ConflictException>(() => service.SaveAsync("companies", null, Request("f01", "Başka Firma"), CancellationToken.None));
        await service.DeactivateAsync("companies", row.Id, CancellationToken.None);
        Assert.Equal(1, await db.Firmalar.CountAsync());
        Assert.Empty(await service.ListAsync("companies", false, CancellationToken.None));
    }

    [Fact]
    public async Task BranchAndDepartment_RequireActiveParentHierarchy()
    {
        await using var db = Context(); var service = new LocalDefinitionService(db);
        var company = await service.SaveAsync("companies", null, Request("F", "Firma"), CancellationToken.None);
        var branchRequest = Request("S", "Şube"); branchRequest.ParentId = company.Id;
        var branch = await service.SaveAsync("branches", null, branchRequest, CancellationToken.None);
        var departmentRequest = Request("D", "Departman"); departmentRequest.ParentId = branch.Id;
        var department = await service.SaveAsync("departments", null, departmentRequest, CancellationToken.None);
        Assert.Equal(branch.Id, department.ParentId);
        await service.DeactivateAsync("branches", branch.Id, CancellationToken.None);
        var inactiveParentRequest = Request("D2", "Departman 2"); inactiveParentRequest.ParentId = branch.Id;
        await Assert.ThrowsAsync<DomainValidationException>(() => service.SaveAsync("departments", null, inactiveParentRequest, CancellationToken.None));
    }

    [Fact]
    public async Task Checklist_CannotBeSavedWithoutItems()
    {
        await using var db = Context(); db.BakimTuruTanimlari.Add(new BakimTuruTanimlari { BakimTuruKodu = "BT", BakimTuruAdi = "Bakım", Aktif = true }); await db.SaveChangesAsync(); var service = new LocalDefinitionService(db);
        var request = Request("KL", "Kontrol Listesi"); request.ParentId = "BT";
        await Assert.ThrowsAsync<DomainValidationException>(() => service.SaveAsync("checklists", null, request, CancellationToken.None));
        request.Items.Add(new ChecklistItemRequest { Text = "Yağı kontrol et", Required = true });
        await service.SaveAsync("checklists", null, request, CancellationToken.None);
        Assert.Single(await db.KontrolListesiMaddeleri.ToListAsync());
    }

    [Fact]
    public async Task HybridWeights_MustTotalOneHundred()
    {
        await using var db = Context(); var service = new LocalDefinitionService(db); var request = Request("H", "Hibrit"); request.AiWeight = 50; request.RuleWeight = 60;
        await Assert.ThrowsAsync<DomainValidationException>(() => service.SaveAsync("hybrid-parameters", null, request, CancellationToken.None));
    }

    [Fact]
    public async Task FailureAndMaintenanceDefinitions_PreserveSelectableFieldsOnEdit()
    {
        await using var db = Context();
        var service = new LocalDefinitionService(db);

        var failureRequest = Request("ELEKTRIK", "Elektrik Arızası");
        failureRequest.Category = "Elektrik";
        failureRequest.Severity = "Yüksek";
        failureRequest.MachineType = "CNC";
        failureRequest.Flag = true;
        var failure = await service.SaveAsync("failure-codes", null, failureRequest, CancellationToken.None);

        Assert.Equal("Elektrik", failure.Type);
        Assert.Equal("Yüksek", failure.Details["severity"]);
        Assert.Equal("CNC", failure.Details["machineType"]);
        Assert.Equal("True", failure.Details["flag"]);

        var maintenanceRequest = Request("ONLEYICI", "Önleyici Bakım");
        maintenanceRequest.Type = "Önleyici";
        maintenanceRequest.Severity = "Orta";
        var maintenance = await service.SaveAsync(
            "maintenance-types", null, maintenanceRequest, CancellationToken.None);

        Assert.Equal("Önleyici", maintenance.Type);
        Assert.Equal("Orta", maintenance.Details["severity"]);
    }

    [Fact]
    public async Task MachineLookup_AllowsComponentCreateAndUpdate()
    {
        await using var db = Context();
        db.Makineler.Add(new Makineler
        {
            MakineId = "MAK-1",
            FirmaId = "FRM-1",
            SubeId = "SUB-1",
            DepartmanId = "DEP-1",
            HatId = "HAT-1",
            MakineKodu = "MAK-001",
            MakineAdi = "Test Makinesi",
            Yas = 1,
            KurulumYili = DateTime.UtcNow.Year,
            Aktif = true,
            OlusturmaTarihi = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new LocalDefinitionService(db);

        var machines = await service.ListAsync("machines", false, CancellationToken.None);
        Assert.Collection(machines, machine =>
        {
            Assert.Equal("MAK-1", machine.Id);
            Assert.Equal("Test Makinesi", machine.Name);
        });

        var create = Request("MTR", "Motor");
        create.ParentId = "MAK-1";
        create.Type = "Elektrik";
        create.Severity = "Yüksek";
        create.EstimatedLife = 10000;
        create.Unit = "saat";
        var component = await service.SaveAsync("components", null, create, CancellationToken.None);

        var update = Request("MTR", "Ana Motor");
        update.ParentId = "MAK-1";
        update.Type = "Elektrik";
        update.Severity = "Kritik";
        update.EstimatedLife = 12000;
        update.Unit = "saat";
        var updated = await service.SaveAsync("components", component.Id, update, CancellationToken.None);

        Assert.Equal("Ana Motor", updated.Name);
        Assert.Equal("MAK-1", updated.ParentId);
        Assert.Equal("Kritik", updated.Details["severity"]);
        Assert.Equal("12000", updated.Details["estimatedLife"]);
        var saved = await db.MakineBilesenler.SingleAsync();
        Assert.Equal("Ana Motor", saved.BilesenAdi);
        Assert.Equal(12000, saved.TahminiOmur);
    }

    private static LocalDefinitionUpsertRequest Request(string code, string name) => new() { Code = code, Name = name, Active = true };
    private static BakimYonetimiDbContext Context() => new(new DbContextOptionsBuilder<BakimYonetimiDbContext>().UseInMemoryDatabase($"definitions-{Guid.NewGuid():N}").Options);
}
