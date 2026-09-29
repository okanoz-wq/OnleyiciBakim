using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;
using System.Globalization;
using System.Text.Json;

namespace OnleyiciBakimSistemi.Controllers;

public sealed class DefinitionsController(ILocalDefinitionClient client) : Controller
{
    public async Task<IActionResult> Index(string module = "companies", bool includeInactive = false, CancellationToken token = default)
    {
        var metadata = await Metadata(module, token);
        return View(new DefinitionIndexViewModel
        {
            Metadata = metadata,
            Rows = await client.ListAsync(module, includeInactive, token),
            IncludeInactive = includeInactive
        });
    }

    [HttpGet]
    public async Task<IActionResult> Table(string module, bool includeInactive, CancellationToken token) =>
        PartialView("_Table", new DefinitionIndexViewModel
        {
            Metadata = await Metadata(module, token),
            Rows = await client.ListAsync(module, includeInactive, token),
            IncludeInactive = includeInactive
        });

    [HttpGet]
    public async Task<IActionResult> Create(string module, CancellationToken token) =>
        PartialView("_CreateOrEditModal", await Prepare(new DefinitionFormViewModel { Module = module, Active = true }, token));

    [HttpGet]
    public async Task<IActionResult> Edit(string module, string id, CancellationToken token)
    {
        var row = await client.GetAsync(module, id, token);
        var form = new DefinitionFormViewModel
        {
            Module = module, Id = row.Id, Code = row.Code, Name = row.Name,
            ParentId = row.ParentId, Type = row.Type, Active = row.Active, Description = row.Description
        };
        if (module == "failure-codes") { form.Category = row.Type; form.Type = null; }
        if (module == "checklists") { form.MachineType = row.Type; form.Type = null; }
        if (row.Details.TryGetValue("startTime", out var start) && TimeOnly.TryParse(start, out var startTime)) form.StartTime = startTime;
        if (row.Details.TryGetValue("endTime", out var end) && TimeOnly.TryParse(end, out var endTime)) form.EndTime = endTime;
        if (row.Details.TryGetValue("aiWeight", out var ai) && int.TryParse(ai, out var aiWeight)) form.AiWeight = aiWeight;
        if (row.Details.TryGetValue("ruleWeight", out var rule) && int.TryParse(rule, out var ruleWeight)) form.RuleWeight = ruleWeight;
        if (row.Details.TryGetValue("minimum", out var min) && decimal.TryParse(min, NumberStyles.Number, CultureInfo.InvariantCulture, out var minimum)) form.Minimum = minimum;
        if (row.Details.TryGetValue("maximum", out var max) && decimal.TryParse(max, NumberStyles.Number, CultureInfo.InvariantCulture, out var maximum)) form.Maximum = maximum;
        if (row.Details.TryGetValue("recommendedDays", out var days) && int.TryParse(days, out var recommendedDays)) form.RecommendedDays = recommendedDays;
        if (row.Details.TryGetValue("maxIncluded", out var maxIncluded) && bool.TryParse(maxIncluded, out var included)) form.MaxIncluded = included;
        if (row.Details.TryGetValue("severity", out var severity)) form.Severity = severity;
        if (row.Details.TryGetValue("unit", out var unit)) form.Unit = unit;
        if (row.Details.TryGetValue("machineType", out var machineType)) form.MachineType = machineType;
        if (row.Details.TryGetValue("flag", out var flag) && bool.TryParse(flag, out var flagValue)) form.Flag = flagValue;
        if (row.Details.TryGetValue("estimatedLife", out var estimatedLife) && int.TryParse(estimatedLife, NumberStyles.Integer, CultureInfo.InvariantCulture, out var life)) form.EstimatedLife = life;
        if (row.Details.TryGetValue("items", out var itemsJson) && !string.IsNullOrWhiteSpace(itemsJson)) form.Items = JsonSerializer.Deserialize<List<ChecklistItemFormViewModel>>(itemsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return PartialView("_CreateOrEditModal", await Prepare(form, token));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string module, string id, CancellationToken token) =>
        PartialView("_DetailsModal", (await Metadata(module, token), await client.GetAsync(module, id, token)));

    [HttpGet]
    public async Task<IActionResult> Delete(string module, string id, CancellationToken token) =>
        PartialView("_DeleteModal", (await Metadata(module, token), await client.GetAsync(module, id, token)));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DefinitionFormViewModel form, CancellationToken token)
    {
        if (form.Module == "hybrid-parameters" && (form.AiWeight ?? 40) + (form.RuleWeight ?? 60) != 100)
            ModelState.AddModelError(string.Empty, "AI ve kural ağırlıklarının toplamı 100 olmalıdır.");
        if (form.Module == "checklists" && form.Items.Count == 0)
            ModelState.AddModelError(string.Empty, "Kontrol listesi en az bir madde içermelidir.");
        if (!ModelState.IsValid) return await Invalid(form, token);
        try
        {
            await client.SaveAsync(form, token);
            return Json(new { success = true, message = "Kayıt yerel SQL Server'a kaydedildi." });
        }
        catch (DefinitionClientException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await Invalid(form, token);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string module, string id, CancellationToken token)
    {
        try { await client.DeactivateAsync(module, id, token); return Json(new { success = true, message = "Kayıt ilişkileri korunarak pasife alındı." }); }
        catch (DefinitionClientException ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    private async Task<DefinitionFormViewModel> Prepare(DefinitionFormViewModel form, CancellationToken token)
    {
        form.Metadata = await Metadata(form.Module, token);
        if (form.Metadata.ParentModule is not null)
            form.Parents = await client.ListAsync(form.Metadata.ParentModule, false, token);
        return form;
    }

    private async Task<DefinitionMetadataViewModel> Metadata(string module, CancellationToken token) =>
        (await client.ModulesAsync(token)).SingleOrDefault(x => x.Module == module)
        ?? throw new DefinitionClientException("Tanım modülü bulunamadı.");

    private async Task<IActionResult> Invalid(DefinitionFormViewModel form, CancellationToken token)
    {
        await Prepare(form, token); Response.StatusCode = 400;
        return PartialView("_CreateOrEditModal", form);
    }
}
