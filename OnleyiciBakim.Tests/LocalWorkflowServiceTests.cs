using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services.Management;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class LocalWorkflowServiceTests
{
    [Fact]
    public async Task MeterReading_CalculatesDifferenceAndRejectsDuplicate()
    {
        await using var db = Context(); Seed(db); var service = new LocalWorkflowService(db);
        var request = Meter();
        await service.AddMeterReadingAsync(request, CancellationToken.None);
        Assert.Equal(35m, (await db.MakineSayacKayitlari.SingleAsync()).SayacFarki);
        await Assert.ThrowsAsync<ConflictException>(() => service.AddMeterReadingAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task MeterReading_RejectsEndSmallerThanStartAndNegativeDuration()
    {
        await using var db = Context(); Seed(db); var service = new LocalWorkflowService(db);
        var smaller = Meter(); smaller.EndValue = 1; smaller.StartValue = 2;
        await Assert.ThrowsAsync<DomainValidationException>(() => service.AddMeterReadingAsync(smaller, CancellationToken.None));
        var negative = Meter(); negative.WorkingMinutes = -1;
        await Assert.ThrowsAsync<DomainValidationException>(() => service.AddMeterReadingAsync(negative, CancellationToken.None));
    }

    [Fact]
    public async Task Intervention_PersistsMultipleChangedParts()
    {
        await using var db = Context(); Seed(db); db.ArizaKartlari.Add(Failure("A-1", "Bildirildi")); await db.SaveChangesAsync(); var service = new LocalWorkflowService(db);
        await service.AddInterventionAsync(new FailureInterventionRequest { FailureId = "A-1", StartedAt = DateTime.Now.AddHours(-1), EndedAt = DateTime.Now, Action = "Rulman değiştirildi", Parts = [new() { PartCode = "P-1", Quantity = 1 }, new() { PartCode = "P-2", Quantity = 2 }] }, CancellationToken.None);
        Assert.Equal(2, await db.ArizaDegisenParcalar.CountAsync());
    }

    [Fact]
    public async Task ClosedFailure_CannotBeIntervenedAgain()
    {
        await using var db = Context(); Seed(db); db.ArizaKartlari.Add(Failure("A-1", "Kapandı")); await db.SaveChangesAsync(); var service = new LocalWorkflowService(db);
        await Assert.ThrowsAsync<ConflictException>(() => service.AddInterventionAsync(new FailureInterventionRequest { FailureId = "A-1", StartedAt = DateTime.Now, Action = "Deneme" }, CancellationToken.None));
    }

    [Fact]
    public async Task Workflow_RejectsUnknownPersonnelAndComponentFromAnotherMachine()
    {
        await using var db = Context();
        Seed(db);
        db.ArizaKartlari.Add(Failure("A-1", "Bildirildi"));
        db.ArizaKoduTanimlari.Add(new ArizaKoduTanimlari
        {
            ArizaKodu = "ARZ",
            ArizaAdi = "Test Arızası",
            Aktif = true
        });
        db.MakineBilesenler.Add(new MakineBilesenler
        {
            MakineId = "M-2",
            BilesenKodu = "B-1",
            BilesenAdi = "Başka Makine Bileşeni",
            Aktif = true
        });
        await db.SaveChangesAsync();
        var service = new LocalWorkflowService(db);

        await Assert.ThrowsAsync<DomainValidationException>(() => service.AddInterventionAsync(
            new FailureInterventionRequest
            {
                FailureId = "A-1",
                StartedAt = DateTime.Now,
                PersonnelId = "BILINMEYEN",
                Action = "Kontrol"
            }, CancellationToken.None));

        var failureCodeId = (await db.ArizaKoduTanimlari.SingleAsync()).ArizaKoduId;
        await Assert.ThrowsAsync<DomainValidationException>(() => service.ReportFailureAsync(
            new FailureReportRequest
            {
                MachineId = "M-1",
                FailureCodeId = failureCodeId,
                ComponentCode = "B-1",
                StartedAt = DateTime.Now.AddMinutes(-5)
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Maintenance_RequiresAllMandatoryChecklistItemsAndLinksPlan()
    {
        await using var db = Context(); Seed(db); var list = new BakimKontrolListeleri { KontrolListesiKodu = "L", KontrolListesiAdi = "Liste", Aktif = true, OlusturmaTarihi = DateTime.UtcNow }; list.KontrolListesiMaddeleri.Add(new KontrolListesiMaddeleri { SiraNo = 1, KontrolMaddesi = "Yağı kontrol et", Zorunlu = true, Aktif = true }); db.Add(list); db.BakimPlanlari.Add(new BakimPlanlari { PlanId = "P-1", MakineId = "M-1", PlanlananTarih = DateTime.Today, TahminiSureDk = 10, Durum = "Planlandı", OlusturmaTarihi = DateTime.UtcNow }); await db.SaveChangesAsync(); var service = new LocalWorkflowService(db);
        var request = new MaintenanceExecutionRequest { PlanId = "P-1", StartedAt = DateTime.Now.AddHours(-1), EndedAt = DateTime.Now, ChecklistId = list.KontrolListesiId };
        await Assert.ThrowsAsync<DomainValidationException>(() => service.CompleteMaintenanceAsync(request, CancellationToken.None));
        request.ChecklistResults.Add(new ChecklistResultRequest { ItemId = list.KontrolListesiMaddeleri.Single().MaddeId, Result = "Uygun" });
        await service.CompleteMaintenanceAsync(request, CancellationToken.None);
        Assert.Equal("P-1", (await db.BakimKayitlari.SingleAsync()).PlanId);
    }

    private static MeterReadingRequest Meter() => new() { MachineId = "M-1", CounterDefinitionId = 1, ShiftId = 1, Date = DateOnly.FromDateTime(DateTime.Today), StartValue = 10, EndValue = 45, WorkingMinutes = 400 };
    private static ArizaKartlari Failure(string id, string status) => new() { ArizaId = id, ArizaTarihi = DateTime.Now.AddHours(-2), MakineId = "M-1", ArizaTuru = "Test", Durum = status, Aktif = status != "Kapandı", VeriKaynagi = "LOCAL", OlusturmaTarihi = DateTime.UtcNow };
    private static void Seed(BakimYonetimiDbContext db)
    {
        db.Makineler.Add(new Makineler { MakineId = "M-1", FirmaId = "F", SubeId = "S", DepartmanId = "D", HatId = "H", MakineKodu = "M", MakineAdi = "Makine", Yas = 1, KurulumYili = DateTime.Today.Year - 1, Durum = "Aktif", Aktif = true, OlusturmaTarihi = DateTime.UtcNow });
        db.MakineSayacTanimlari.Add(new MakineSayacTanimlari { SayacTanimId = 1, SayacKodu = "S", SayacAdi = "Saat", SayacTuru = "Çalışma", Birim = "saat", Aktif = true });
        db.Vardiyalar.Add(new Vardiyalar { VardiyaId = 1, VardiyaKodu = "V", VardiyaAdi = "Vardiya", BaslangicSaati = new TimeOnly(8, 0), BitisSaati = new TimeOnly(16, 0), Aktif = true }); db.SaveChanges();
    }
    private static BakimYonetimiDbContext Context() => new(new DbContextOptionsBuilder<BakimYonetimiDbContext>().UseInMemoryDatabase($"workflow-{Guid.NewGuid():N}").Options);
}
