using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OnleyiciBakim.Contracts.Management;
using OnleyiciBakim.Data.BakimYonetimi;
using OnleyiciBakim.Data.BakimYonetimi.Entities;
using OnleyiciBakim.Infrastructure;

namespace OnleyiciBakim.Services.Management;

public interface ILocalWorkflowService
{
    Task<WorkflowResult> AddMeterReadingAsync(MeterReadingRequest request, CancellationToken token);
    Task<WorkflowResult> AddCalendarAsync(WorkCalendarRequest request, CancellationToken token);
    Task<WorkflowResult> TransferMachineAsync(MachineTransferRequest request, CancellationToken token);
    Task<WorkflowResult> ReportFailureAsync(FailureReportRequest request, CancellationToken token);
    Task<WorkflowResult> AddInterventionAsync(FailureInterventionRequest request, CancellationToken token);
    Task<WorkflowResult> CloseFailureAsync(FailureCloseRequest request, CancellationToken token);
    Task<WorkflowResult> CompleteMaintenanceAsync(MaintenanceExecutionRequest request, CancellationToken token);
    Task<WorkflowResult> CreateWorkOrderAsync(WorkOrderRequest request, CancellationToken token);
    Task<WorkflowResult> ChangeWorkOrderStatusAsync(int id, StatusChangeRequest request, CancellationToken token);
    Task<WorkflowResult> ProcessWarningAsync(string id, WarningActionRequest request, CancellationToken token);
}

public sealed class LocalWorkflowService(BakimYonetimiDbContext db) : ILocalWorkflowService
{
    public async Task<WorkflowResult> AddMeterReadingAsync(MeterReadingRequest r, CancellationToken t)
    {
        if (r.StartValue < 0 || r.EndValue < 0 || r.WorkingMinutes < 0 || r.IdleMinutes < 0 || r.ProductionMinutes < 0)
            throw new DomainValidationException("Sayaç değerleri ve süreler negatif olamaz.");
        if (r.Date > DateOnly.FromDateTime(DateTime.Today)) throw new DomainValidationException("Gelecek tarih için sayaç kaydı girilemez.");
        if (r.EndValue < r.StartValue) throw new DomainValidationException("Bitiş sayacı başlangıç sayacından küçük olamaz.");
        if (!await db.Makineler.AnyAsync(x => x.MakineId == r.MachineId && x.Aktif, t) ||
            !await db.MakineSayacTanimlari.AnyAsync(x => x.SayacTanimId == r.CounterDefinitionId && x.Aktif, t) ||
            !await db.Vardiyalar.AnyAsync(x => x.VardiyaId == r.ShiftId && x.Aktif, t)) throw new DomainValidationException("Makine, sayaç veya vardiya seçimi geçersiz.");
        if (await db.MakineSayacKayitlari.AnyAsync(x => x.MakineId == r.MachineId && x.SayacTanimId == r.CounterDefinitionId && x.Tarih == r.Date && x.VardiyaId == r.ShiftId, t)) throw new ConflictException("Aynı makine, sayaç, tarih ve vardiya için kayıt zaten var.");
        var entity = new MakineSayacKayitlari { MakineId = r.MachineId, SayacTanimId = r.CounterDefinitionId, Tarih = r.Date, VardiyaId = r.ShiftId, BaslangicSayac = r.StartValue, BitisSayac = r.EndValue, SayacFarki = r.EndValue - r.StartValue, CalismaSuresiDk = r.WorkingMinutes, BostaKalmaSuresiDk = r.IdleMinutes, UretimSuresiDk = r.ProductionMinutes, UretimMiktari = r.ProductionQuantity, VeriKaynagi = "LOCAL", Aciklama = r.Description, OlusturmaTarihi = DateTime.UtcNow };
        db.MakineSayacKayitlari.Add(entity); await db.SaveChangesAsync(t); return new(entity.SayacKayitId.ToString(), "Sayaç farkı sunucu tarafından hesaplanarak kaydedildi.", r.MachineId);
    }

    public async Task<WorkflowResult> AddCalendarAsync(WorkCalendarRequest r, CancellationToken t)
    {
        if (await db.MakineCalismaTakvimleri.AnyAsync(x => x.MakineId == r.MachineId && x.Tarih == r.Date && x.VardiyaId == r.ShiftId, t)) throw new ConflictException("Bu makine, tarih ve vardiya için takvim kaydı zaten var.");
        var entity = new MakineCalismaTakvimleri { MakineId = r.MachineId, Tarih = r.Date, VardiyaId = r.ShiftId, PlanlananCalismaSuresiDk = r.PlannedWorkingMinutes, PlanlananUretimMiktari = r.PlannedProductionQuantity, Durum = r.Status, Aciklama = r.Description, OlusturmaTarihi = DateTime.UtcNow };
        db.Add(entity); await db.SaveChangesAsync(t); return new(entity.Id.ToString(), "Çalışma takvimi kaydedildi.");
    }

    public async Task<WorkflowResult> TransferMachineAsync(MachineTransferRequest r, CancellationToken t)
    {
        var machine = await db.Makineler.SingleOrDefaultAsync(x => x.MakineId == r.MachineId && x.Aktif, t) ?? throw NotFound("Makine");
        await EnsurePersonnelAsync(r.PersonnelId, t);
        if (!await db.UretimHatlari.AnyAsync(x => x.HatId == r.WorkCenterId && x.DepartmanId == r.DepartmentId && x.Departman.SubeId == r.BranchId && x.Departman.Sube.FirmaId == r.CompanyId && x.AktifMi, t)) throw new DomainValidationException("Yeni organizasyon hiyerarşisi geçersiz.");
        await using var tx = await Begin(t);
        var entity = new MakineTransferleri { MakineId = machine.MakineId, EskiFirmaId = machine.FirmaId, EskiSubeId = machine.SubeId, EskiDepartmanId = machine.DepartmanId, EskiHatId = machine.HatId, YeniFirmaId = r.CompanyId, YeniSubeId = r.BranchId, YeniDepartmanId = r.DepartmentId, YeniHatId = r.WorkCenterId, TransferTarihi = r.TransferAt, Gerekce = r.Reason.Trim(), IslemiYapanPersonelId = r.PersonnelId, OlusturmaTarihi = DateTime.UtcNow };
        db.Add(entity); machine.FirmaId = r.CompanyId; machine.SubeId = r.BranchId; machine.DepartmanId = r.DepartmentId; machine.HatId = r.WorkCenterId; machine.GuncellemeTarihi = DateTime.UtcNow; await db.SaveChangesAsync(t); await Commit(tx, t); return new(entity.TransferId.ToString(), "Makine transferi ve güncel organizasyon bilgisi birlikte kaydedildi.");
    }

    public async Task<WorkflowResult> ReportFailureAsync(FailureReportRequest r, CancellationToken t)
    {
        if (r.StartedAt > DateTime.Now) throw new DomainValidationException("Arıza başlangıcı gelecek tarih olamaz.");
        var code = await db.ArizaKoduTanimlari.AsNoTracking().SingleOrDefaultAsync(x => x.ArizaKoduId == r.FailureCodeId && x.Aktif, t) ?? throw new DomainValidationException("Arıza kodu bulunamadı veya pasif.");
        if (!await db.Makineler.AnyAsync(x => x.MakineId == r.MachineId && x.Aktif, t)) throw NotFound("Makine");
        await EnsurePersonnelAsync(r.ReporterPersonnelId, t);
        if (!string.IsNullOrWhiteSpace(r.ComponentCode) &&
            !await db.MakineBilesenler.AnyAsync(x =>
                x.MakineId == r.MachineId && x.BilesenKodu == r.ComponentCode && x.Aktif, t))
            throw new DomainValidationException("Seçilen bileşen bu makineye ait değil veya pasif.");
        var entity = new ArizaKartlari { ArizaId = NewId("ARZ"), ArizaTarihi = r.StartedAt, MakineId = r.MachineId, ArizaKoduId = code.ArizaKoduId, VardiyaId = r.ShiftId, BilesenKodu = r.ComponentCode, ArizaTuru = code.ArizaAdi, OnemSeviyesi = r.Severity ?? code.VarsayilanOnemSeviyesi, Durum = "Bildirildi", IsEmriNo = r.WorkOrderNo, UrunStokKodu = r.ProductCode, IlkBelirti = r.FirstSymptom, UretimDurduMu = r.ProductionStopped || code.UretimiDurdururMu, TahminiDurusSuresiDk = r.EstimatedDowntimeMinutes, BildirenPersonelId = r.ReporterPersonnelId, Aciklama = r.Description, Aktif = true, VeriKaynagi = "LOCAL", OlusturmaTarihi = DateTime.UtcNow };
        db.Add(entity); await db.SaveChangesAsync(t); return new(entity.ArizaId, "Arıza müdahale bilgisi zorunlu tutulmadan bildirildi.", r.MachineId);
    }

    public async Task<WorkflowResult> AddInterventionAsync(FailureInterventionRequest r, CancellationToken t)
    {
        var failure = await db.ArizaKartlari.SingleOrDefaultAsync(x => x.ArizaId == r.FailureId, t) ?? throw NotFound("Arıza"); if (failure.Durum == "Kapandı") throw new ConflictException("Kapanmış arıza değiştirilemez."); if (r.EndedAt < r.StartedAt) throw new DomainValidationException("Müdahale bitişi başlangıçtan önce olamaz.");
        await EnsurePersonnelAsync(r.PersonnelId, t);
        await using var tx = await Begin(t); var intervention = new ArizaMudahaleler { ArizaId = r.FailureId, MudahaleBaslangic = r.StartedAt, MudahaleBitis = r.EndedAt, PersonelId = r.PersonnelId, YapilanIslem = r.Action, KokNeden = r.RootCause, Sonuc = r.Result, GercekDurusSuresiDk = r.ActualDowntimeMinutes, Aciklama = r.Description, Durum = r.EndedAt.HasValue ? "Tamamlandı" : "Devam Ediyor" }; db.Add(intervention); failure.Durum = "Müdahale Ediliyor"; await db.SaveChangesAsync(t);
        db.ArizaDegisenParcalar.AddRange(r.Parts.Select(x => new ArizaDegisenParcalar { MudahaleId = intervention.MudahaleId, ParcaId = x.PartId, ParcaKodu = x.PartCode, Miktar = x.Quantity, Aciklama = x.Description })); await db.SaveChangesAsync(t); await Commit(tx, t); return new(intervention.MudahaleId.ToString(), "Müdahale ve değişen parçalar birlikte kaydedildi.", failure.MakineId);
    }

    public async Task<WorkflowResult> CloseFailureAsync(FailureCloseRequest r, CancellationToken t)
    {
        await EnsurePersonnelAsync(r.ClosingPersonnelId, t, required: true);
        var x = await db.ArizaKartlari.SingleOrDefaultAsync(y => y.ArizaId == r.FailureId, t) ?? throw NotFound("Arıza"); if (x.Durum == "Kapandı") throw new ConflictException("Arıza zaten kapatılmış."); if (r.FailureEndedAt < x.ArizaTarihi || r.ClosedAt < r.FailureEndedAt) throw new DomainValidationException("Kapanış tarihleri arıza başlangıcından önce olamaz."); x.ArizaBitisTarihi = r.FailureEndedAt; x.KapanisTarihi = r.ClosedAt; x.KapanisPersonelId = r.ClosingPersonnelId; x.CozumAciklamasi = r.Resolution; x.UretimKaybiMiktari = r.ProductionLoss; x.UretimKaybiBirimi = r.ProductionLossUnit; x.Durum = r.FinalStatus; x.Aktif = false; x.DurusSuresiDk = (int)Math.Max(0, (r.FailureEndedAt - x.ArizaTarihi).TotalMinutes); await db.SaveChangesAsync(t); return new(x.ArizaId, "Arıza kapatıldı ve normal düzenlemeye kilitlendi.", x.MakineId);
    }

    public async Task<WorkflowResult> CompleteMaintenanceAsync(MaintenanceExecutionRequest r, CancellationToken t)
    {
        var plan = await db.BakimPlanlari.SingleOrDefaultAsync(x => x.PlanId == r.PlanId, t) ?? throw NotFound("Bakım planı"); if (plan.Durum is "Tamamlandı" or "İptal") throw new ConflictException("Kapalı bakım planı yeniden tamamlanamaz."); if (r.EndedAt < r.StartedAt) throw new DomainValidationException("Bakım bitişi başlangıçtan önce olamaz.");
        await EnsurePersonnelAsync(r.PersonnelId, t);
        if (r.ChecklistId.HasValue) { var required = await db.KontrolListesiMaddeleri.AsNoTracking().Where(x => x.KontrolListesiId == r.ChecklistId && x.Aktif && x.Zorunlu).Select(x => x.MaddeId).ToListAsync(t); var completed = r.ChecklistResults.Where(x => !string.IsNullOrWhiteSpace(x.Result)).Select(x => x.ItemId).ToHashSet(); if (required.Any(x => !completed.Contains(x))) throw new DomainValidationException("Zorunlu kontrol maddeleri tamamlanmadan bakım kapatılamaz."); }
        await using var tx = await Begin(t); var maintenance = new BakimKayitlari { BakimId = NewId("BKM"), BakimTarihi = r.EndedAt, MakineId = plan.MakineId, PlanId = plan.PlanId, BakimTuruId = plan.BakimTuruId, BakimTuru = plan.BakimTuru, BakimNedeni = plan.Gerekce, BaslangicTarihi = r.StartedAt, BitisTarihi = r.EndedAt, SureDk = (int)(r.EndedAt - r.StartedAt).TotalMinutes, Durum = "Tamamlandı", Sonuc = r.Result, PersonelId = r.PersonnelId, MaliyetTl = r.Cost, KaynakTuru = plan.AnalizId.HasValue ? "Hibrit Risk" : "Manuel Plan", MakineSonrasiDurum = r.MachineStatusAfter, KontrolListesiId = r.ChecklistId, VeriKaynagi = "LOCAL", OlusturmaTarihi = DateTime.UtcNow }; db.Add(maintenance); await db.SaveChangesAsync(t);
        if (r.ChecklistId.HasValue) { var items = await db.KontrolListesiMaddeleri.AsNoTracking().Where(x => x.KontrolListesiId == r.ChecklistId && x.Aktif).ToListAsync(t); db.BakimKontrolSonuclari.AddRange(items.Select((x, i) => { var result = r.ChecklistResults.FirstOrDefault(y => y.ItemId == x.MaddeId); return new BakimKontrolSonuclari { BakimId = maintenance.BakimId, MaddeId = x.MaddeId, SiraNo = i + 1, KontrolMaddesi = x.KontrolMaddesi, Sonuc = result?.Result, Aciklama = result?.Description }; })); }
        var old = plan.Durum; plan.Durum = "Tamamlandı"; plan.GuncellemeTarihi = DateTime.UtcNow; db.BakimPlanDurumGecmisi.Add(new BakimPlanDurumGecmisi { PlanId = plan.PlanId, EskiDurum = old, YeniDurum = plan.Durum, PersonelId = r.PersonnelId, DegisiklikTarihi = DateTime.UtcNow, Aciklama = "Bakım gerçekleşme kaydı oluşturuldu." }); var machine = await db.Makineler.SingleAsync(x => x.MakineId == plan.MakineId, t); machine.SonBakimTarihi = r.EndedAt; await db.SaveChangesAsync(t); await Commit(tx, t); return new(maintenance.BakimId, "Bakım, kontrol sonuçları ve plan geçmişi birlikte kaydedildi.", plan.MakineId);
    }

    public async Task<WorkflowResult> CreateWorkOrderAsync(WorkOrderRequest r, CancellationToken t)
    {
        await EnsurePersonnelAsync(r.ResponsiblePersonnelId, t);
        if (!await db.Makineler.AnyAsync(x => x.MakineId == r.MachineId && x.Aktif, t)) throw NotFound("Makine"); var entity = new IsEmirleri { IsEmriNo = $"IE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25], MakineId = r.MachineId, PlanId = r.PlanId, ArizaId = r.FailureId, DevirKaynagi = r.WarningId is not null ? "Erken Uyarı" : r.PlanId is not null ? "Bakım Planı" : r.FailureId is not null ? "Arıza" : "LOCAL", Oncelik = r.Priority, PlanlananTarih = r.PlannedDate, SorumluPersonelId = r.ResponsiblePersonnelId, Durum = "Açık", ErpSistemi = "LOCAL", ErpDurumu = null, OlusturmaTarihi = DateTime.UtcNow, Aciklama = r.Description }; db.Add(entity); await db.SaveChangesAsync(t); db.IsEmriDurumGecmisi.Add(new IsEmriDurumGecmisi { IsEmriId = entity.IsEmriId, YeniDurum = "Açık", Tarih = DateTime.UtcNow, PersonelId = r.ResponsiblePersonnelId, Aciklama = "Yerel iş emri oluşturuldu." }); await db.SaveChangesAsync(t); return new(entity.IsEmriId.ToString(), "Yerel iş emri oluşturuldu.");
    }

    public async Task<WorkflowResult> ChangeWorkOrderStatusAsync(int id, StatusChangeRequest r, CancellationToken t)
    {
        await EnsurePersonnelAsync(r.PersonnelId, t);
        var x = await db.IsEmirleri.SingleOrDefaultAsync(y => y.IsEmriId == id, t) ?? throw NotFound("İş emri"); var allowed = new[] { "Açık", "Atandı", "Devam Ediyor", "Tamamlandı", "İptal" }; if (!allowed.Contains(r.Status)) throw new DomainValidationException("Geçersiz iş emri durumu."); var old = x.Durum; x.Durum = r.Status; db.IsEmriDurumGecmisi.Add(new IsEmriDurumGecmisi { IsEmriId = id, EskiDurum = old, YeniDurum = r.Status, Tarih = DateTime.UtcNow, PersonelId = r.PersonnelId, Aciklama = r.Description }); await db.SaveChangesAsync(t); return new(id.ToString(), "İş emri durumu ve geçmişi güncellendi.");
    }

    public async Task<WorkflowResult> ProcessWarningAsync(string id, WarningActionRequest r, CancellationToken t)
    {
        await EnsurePersonnelAsync(r.PersonnelId, t);
        var warning = await db.Uyarilar.SingleOrDefaultAsync(x => x.UyariId == id, t) ?? throw NotFound("Uyarı"); if (warning.Durum == "Kapalı") throw new ConflictException("Kapalı uyarı üzerinde işlem yapılamaz."); var old = warning.Durum; var action = r.Action.Trim().ToLowerInvariant();
        switch (action) { case "assign": warning.SorumluPersonelId = r.PersonnelId ?? throw new DomainValidationException("Sorumlu personel zorunludur."); warning.Durum = "Atandı"; break; case "close": warning.Durum = "Kapalı"; warning.KapatmaTarihi = DateTime.UtcNow; warning.KapatmaNedeni = r.Reason; break; case "no-action": if (string.IsNullOrWhiteSpace(r.Reason)) throw new DomainValidationException("İşlem gerekmiyor kararı için gerekçe zorunludur."); warning.Durum = "Kapalı"; warning.KapatmaTarihi = DateTime.UtcNow; warning.KapatmaNedeni = r.Reason; warning.IslemSonucu = "İşlem gerekmiyor"; break; case "plan": await EnsureWarningPlan(warning, r, t); warning.Durum = "İşlemde"; break; default: throw new DomainValidationException("Geçersiz uyarı işlemi."); }
        warning.IslemYapanPersonelId = r.PersonnelId; db.UyariIslemGecmisi.Add(new UyariIslemGecmisi { UyariId = id, IslemTuru = r.Action, EskiDurum = old, YeniDurum = warning.Durum, PersonelId = r.PersonnelId, IslemTarihi = DateTime.UtcNow, Aciklama = r.Reason }); await db.SaveChangesAsync(t); return new(id, "Uyarı işlemi ve geçmişi kaydedildi.");
    }

    private async Task EnsureWarningPlan(Uyarilar warning, WarningActionRequest r, CancellationToken t)
    {
        var active = new[] { "Taslak", "Planlandı", "Onaylandı", "Devam Ediyor" }; var plan = await db.BakimPlanlari.FirstOrDefaultAsync(x => x.MakineId == warning.MakineId && x.AnalizId != null && active.Contains(x.Durum!), t); if (plan is null) { plan = new BakimPlanlari { PlanId = NewId("PLN"), MakineId = warning.MakineId, AnalizId = warning.AnalizId, UyariId = warning.UyariId, OnerilenTarih = r.PlannedDate, PlanlananTarih = (r.PlannedDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7))).ToDateTime(TimeOnly.MinValue), BakimTuru = "Planlı", Oncelik = warning.UyariSeviyesi, TahminiSureDk = 0, Durum = "Taslak", OnayDurumu = "Onay Bekliyor", Gerekce = r.Reason ?? warning.OnerilenAksiyon, VeriKaynagi = "LOCAL", OlusturmaTarihi = DateTime.UtcNow }; db.Add(plan); await db.SaveChangesAsync(t); } warning.PlanId = plan.PlanId;
    }

    private async Task<IDbContextTransaction?> Begin(CancellationToken t) => db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(t) : null;
    private async Task EnsurePersonnelAsync(string? personnelId, CancellationToken token, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(personnelId))
        {
            if (required) throw new DomainValidationException("Personel seçimi zorunludur.");
            return;
        }
        if (!await db.Personeller.AsNoTracking().AnyAsync(x => x.PersonelId == personnelId && x.AktifMi, token))
            throw new DomainValidationException("Seçilen personel bulunamadı veya pasif.");
    }
    private static Task Commit(IDbContextTransaction? tx, CancellationToken t) => tx is null ? Task.CompletedTask : tx.CommitAsync(t);
    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20];
    private static Exception NotFound(string resource) => new ResourceNotFoundException($"{resource} bulunamadı.");
}
