using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;
using System.Text.Json;

namespace OnleyiciBakimSistemi.Controllers;

public sealed class OperationsController(
    IMaintenanceDataService data,
    ILocalWorkflowClient workflows,
    ILocalReportClient reports,
    ILocalDefinitionClient definitions) : Controller
{
    private static readonly Dictionary<string, (string Title, string Description, string? Action)> Modules = new()
    {
        ["meter-readings"] = ("Sayaç / Çalışma Verileri", "Sayaç farkı otomatik hesaplanır; mükerrer vardiya kayıtları engellenir.", "Çalışma Verisi Gir"),
        ["work-calendar"] = ("Makine Çalışma Takvimi", "Makine ve vardiya bazlı planlanan çalışma kayıtları.", "Takvim Kaydı Ekle"),
        ["transfers"] = ("Makine Transferleri", "Organizasyon transferi geçmişle birlikte transaction içinde tamamlanır.", "Makine Transfer Et"),
        ["failure-report"] = ("Arıza Bildirimi", "İlk bildirimde henüz bilinmeyen müdahale ve çözüm alanları istenmez.", "Arıza Bildir"),
        ["failure-actions"] = ("Müdahale ve Kapatma", "Açık arızalara müdahale ekleyin veya çözüm bilgileriyle kapatın.", "Müdahale Ekle"),
        ["maintenance-execution"] = ("Bakım Gerçekleştirme", "Planı gerçekleşen bakıma bağlar; zorunlu kontrol maddelerini doğrular.", "Bakımı Tamamla"),
        ["work-orders"] = ("İş Emirleri", "Bakım planı, arıza veya erken uyarı kaynaklı yerel iş emri.", "İş Emri Oluştur"),
        ["alerts"] = ("Erken Uyarılar", "Sorumlu atama, plan oluşturma, işlem gerekmiyor ve kapatma akışları.", "Uyarı İşlemi"),
        ["risk-analysis"] = ("Hibrit Risk Analizi", "Sistem tarafından üretilen analizler salt okunur gösterilir; normal akış otomatiktir.", null),
        ["decisions"] = ("Karar Geçmişi", "Sistem önerileri ve kullanıcı kararlarının salt okunur geçmişi.", null),
        ["maintenance-history"] = ("Bakım Geçmişi", "Gerçekleşen bakım kayıtları ve bağlı planlar.", null),
        ["maintenance-calendar"] = ("Bakım Takvimi", "PlanlananTarih üzerinden bakım planları.", null),
        ["failure-reporting"] = ("Arıza Analizi", "Gerçek arıza kayıtlarından üretilen rapor.", null),
        ["maintenance-reporting"] = ("Bakım Performansı", "Planlanan ve gerçekleşen bakım ilişkisine dayalı rapor.", null),
        ["model-comparison"] = ("Model ve Kural Karşılaştırma", "Kural, AI ve nihai hibrit skor karşılaştırması.", null)
    };

    public async Task<IActionResult> Index(string module, CancellationToken token)
    {
        var meta = Meta(module); return View(new OperationsIndexViewModel { Module = module, Title = meta.Title, Description = meta.Description, ActionLabel = meta.Action, Machines = await data.GetMachinesAsync(token), Faults = await data.GetFaultsAsync(token), Plans = await data.GetMaintenancePlansAsync(token), Report = await reports.GetAsync(module, token) });
    }

    [HttpGet]
    public async Task<IActionResult> PlanDetails(string id, CancellationToken token) => PartialView("_PlanDetailsModal", (await data.GetMaintenancePlansAsync(token)).Single(x => x.Id == id));

    [HttpGet]
    public async Task<IActionResult> FaultDetails(string id, CancellationToken token) => PartialView("_FaultDetailsModal", (await data.GetFaultsAsync(token)).Single(x => x.Id == id));

    [HttpGet]
    public async Task<IActionResult> ActionModal(string module, CancellationToken token) => PartialView("_ActionModal", await Prepare(new OperationFormViewModel { Module = module, Title = Meta(module).Title, Date = DateOnly.FromDateTime(DateTime.Today), StartedAt = DateTime.Now }, token));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(OperationFormViewModel form, CancellationToken token)
    {
        if (!ModelState.IsValid)
        {
            await Prepare(form, token);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return PartialView("_ActionModal", form);
        }
        try
        {
            var (endpoint, payload) = Payload(form);
            var message = await workflows.PostAsync(endpoint, payload, token);
            return Json(new { success = true, message });
        }
        catch (Exception ex) when (ex is LocalWorkflowClientException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message); await Prepare(form, token); Response.StatusCode = 400; return PartialView("_ActionModal", form);
        }
    }

    private static (string, object) Payload(OperationFormViewModel f) => f.Module switch
    {
        "meter-readings" => ("meter-readings", new { machineId = Required(f.MachineId, "Makine"), counterDefinitionId = f.CounterDefinitionId, date = f.Date, shiftId = f.ShiftId, startValue = f.StartValue, endValue = f.EndValue, workingMinutes = f.Minutes, productionQuantity = f.Quantity, description = f.Description }),
        "work-calendar" => ("work-calendar", new { machineId = Required(f.MachineId, "Makine"), date = f.Date, shiftId = f.ShiftId, plannedWorkingMinutes = f.Minutes, plannedProductionQuantity = f.Quantity, status = f.Status ?? "Planlandı", description = f.Description }),
        "transfers" => ("transfers", new { machineId = Required(f.MachineId, "Makine"), companyId = Required(f.CompanyId, "Firma"), branchId = Required(f.BranchId, "Şube"), departmentId = Required(f.DepartmentId, "Departman"), workCenterId = Required(f.WorkCenterId, "İş merkezi"), transferAt = f.StartedAt ?? DateTime.Now, reason = Required(f.Description, "Gerekçe"), personnelId = f.PersonnelId }),
        "failure-report" => ("failures", new { machineId = Required(f.MachineId, "Makine"), failureCodeId = f.FailureCodeId, shiftId = f.ShiftId, componentCode = f.ComponentCode, startedAt = f.StartedAt ?? DateTime.Now, severity = f.Severity, firstSymptom = f.Description, estimatedDowntimeMinutes = f.Minutes, reporterPersonnelId = f.PersonnelId }),
        "failure-actions" when f.Status == "Kapandı" => ("failures/close", new { failureId = Required(f.FailureId, "Arıza"), closedAt = f.EndedAt ?? DateTime.Now, failureEndedAt = f.EndedAt ?? DateTime.Now, closingPersonnelId = Required(f.PersonnelId, "Personel"), resolution = Required(f.Description, "Çözüm"), productionLoss = f.Quantity, finalStatus = "Kapandı" }),
        "failure-actions" => ("interventions", new { failureId = Required(f.FailureId, "Arıza"), startedAt = f.StartedAt ?? DateTime.Now, endedAt = f.EndedAt, personnelId = f.PersonnelId, action = Required(f.Description, "Yapılan işlem"), actualDowntimeMinutes = f.Minutes, parts = string.IsNullOrWhiteSpace(f.PartCode) ? Array.Empty<object>() : new object[] { new { partCode = f.PartCode, quantity = f.Quantity ?? 1 } } }),
        "maintenance-execution" => ("maintenance-executions", new { planId = Required(f.PlanId, "Plan"), startedAt = f.StartedAt ?? DateTime.Now, endedAt = f.EndedAt ?? DateTime.Now, personnelId = f.PersonnelId, result = f.Description, machineStatusAfter = f.Status, checklistId = f.ChecklistId, cost = f.Cost, checklistResults = f.ChecklistResults }),
        "work-orders" => ("work-orders", new { machineId = Required(f.MachineId, "Makine"), planId = f.PlanId, failureId = f.FailureId, warningId = f.WarningId, priority = f.Severity, plannedDate = f.Date, responsiblePersonnelId = f.PersonnelId, description = f.Description }),
        "alerts" => ($"warnings/{Required(f.WarningId, "Uyarı")}/actions", new { action = f.Status ?? "assign", personnelId = f.PersonnelId, reason = f.Description, plannedDate = f.Date }),
        _ => throw new InvalidOperationException("Bu ekran salt okunurdur.")
    };

    private async Task<OperationFormViewModel> Prepare(OperationFormViewModel f, CancellationToken t)
    {
        f.Machines = await data.GetMachinesAsync(t);
        f.Faults = await data.GetFaultsAsync(t);
        f.Plans = await data.GetMaintenancePlansAsync(t);

        if (f.Module is "transfers" or "failure-report" or "failure-actions" or
            "maintenance-execution" or "work-orders" or "alerts")
            f.Personnel = await definitions.ListAsync("personnel", false, t);

        if (f.Module == "meter-readings")
            f.Counters = await definitions.ListAsync("counters", false, t);
        if (f.Module is "meter-readings" or "work-calendar" or "failure-report")
            f.Shifts = await definitions.ListAsync("shifts", false, t);
        if (f.Module == "failure-report")
        {
            f.FailureCodes = await definitions.ListAsync("failure-codes", false, t);
            f.Components = await definitions.ListAsync("components", false, t);
        }
        if (f.Module == "maintenance-execution")
        {
            f.Checklists = await definitions.ListAsync("checklists", false, t);
            var checklistDefinitions = new List<OperationChecklistViewModel>();
            foreach (var checklist in f.Checklists)
            {
                var details = await definitions.GetAsync("checklists", checklist.Id, t);
                var items = details.Details.TryGetValue("items", out var itemsJson) &&
                            !string.IsNullOrWhiteSpace(itemsJson)
                    ? JsonSerializer.Deserialize<List<OperationChecklistItemViewModel>>(
                          itemsJson,
                          new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []
                    : [];
                checklistDefinitions.Add(new OperationChecklistViewModel
                {
                    Id = checklist.Id,
                    Code = checklist.Code,
                    Name = checklist.Name,
                    Items = items
                });
            }
            f.ChecklistDefinitions = checklistDefinitions;
        }
        if (f.Module == "transfers")
        {
            f.Companies = await definitions.ListAsync("companies", false, t);
            f.Branches = await definitions.ListAsync("branches", false, t);
            f.Departments = await definitions.ListAsync("departments", false, t);
            f.WorkCenters = await definitions.ListAsync("work-centers", false, t);
        }
        if (f.Module is "work-orders" or "alerts")
            f.Warnings = await definitions.ListAsync("warnings", false, t);
        if (f.Module == "failure-actions")
            f.Parts = await definitions.ListAsync("parts", false, t);
        return f;
    }
    private static (string Title, string Description, string? Action) Meta(string module) => Modules.TryGetValue(module, out var value) ? value : throw new InvalidOperationException("İşlem modülü bulunamadı.");
    private static string Required(string? value, string label) => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"{label} seçimi zorunludur.");
}
