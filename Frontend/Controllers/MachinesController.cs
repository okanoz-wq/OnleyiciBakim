using Microsoft.AspNetCore.Mvc;
using OnleyiciBakimSistemi.Models.ViewModels;
using OnleyiciBakimSistemi.Services;

namespace OnleyiciBakimSistemi.Controllers;

public sealed class MachinesController(
    IMaintenanceDataService data,
    IMachineManagementClient management) : Controller
{
    public async Task<IActionResult> Index(
        string? search, string? status, string? criticality, string? source,
        string sortBy = "risk", bool descending = true, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var vm = await BuildListAsync(search, status, criticality, source, sortBy, descending, page, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Table(
        string? search, string? status, string? criticality, string? source,
        string sortBy = "risk", bool descending = true, int page = 1,
        CancellationToken cancellationToken = default) =>
        PartialView("_Table", await BuildListAsync(
            search, status, criticality, source, sortBy, descending, page, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var lookups = await management.GetLookupsAsync(cancellationToken);
        if (lookups.UseErpAsMasterSource)
            return Conflict("ERP ana veri kaynağı modunda yerel makine oluşturulamaz.");
        return PartialView("_CreateOrEditModal", new MachineFormViewModel { Lookups = lookups });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MachineFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return await InvalidFormAsync(form, cancellationToken);
        try
        {
            var result = await management.CreateAsync(form, cancellationToken);
            return Json(new { result.Success, result.Message, result.Id });
        }
        catch (BackendManagementException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await InvalidFormAsync(form, cancellationToken);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var details = await management.GetDetailsAsync(id, cancellationToken);
        if (!details.CanManage) return Conflict("ERP tarafından yönetilen makine değiştirilemez.");
        return PartialView("_CreateOrEditModal", await ToFormAsync(details, cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MachineFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Id)) return BadRequest("Makine kimliği eksik.");
        if (!ModelState.IsValid) return await InvalidFormAsync(form, cancellationToken);
        try
        {
            var result = await management.UpdateAsync(form.Id, form, cancellationToken);
            return Json(new { result.Success, result.Message, result.Id });
        }
        catch (BackendManagementException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await InvalidFormAsync(form, cancellationToken);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DetailsModal(string id, CancellationToken cancellationToken) =>
        PartialView("_DetailsModal", await management.GetDetailsAsync(id, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var details = await management.GetDetailsAsync(id, cancellationToken);
        if (!details.CanManage) return Conflict("ERP tarafından yönetilen makine pasife alınamaz.");
        return PartialView("_DeleteModal", new MachineDeleteViewModel
        {
            Id = details.Id, Code = details.Code, Name = details.Name,
            HasRelatedRecords = details.HasRelatedRecords,
            RelatedRecordCount = details.ComponentCount + details.FailureCount +
                                 details.MaintenanceCount + details.RiskAnalysisCount,
            Status = details.Status
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await management.DeactivateAsync(id, cancellationToken);
            return Json(new { result.Success, result.Message, result.Id, result.Deactivated });
        }
        catch (BackendManagementException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await management.ActivateAsync(id, cancellationToken);
            return Json(new { result.Success, result.Message, result.Id });
        }
        catch (BackendManagementException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> Details(
        string id, int faultPage = 1, CancellationToken cancellationToken = default)
    {
        var machine = await data.GetMachineAsync(id, cancellationToken);
        if (machine is null) return NotFound();
        var allFaults = (await data.GetFaultsByMachineAsync(id, cancellationToken))
            .OrderByDescending(f => f.Date).ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(
            allFaults.Count / (double)PaginationViewModel.DefaultPageSize));
        faultPage = Math.Clamp(faultPage, 1, totalPages);
        return View(new MachineDetailViewModel
        {
            Machine = machine,
            Faults = allFaults.Skip((faultPage - 1) * PaginationViewModel.DefaultPageSize)
                .Take(PaginationViewModel.DefaultPageSize).ToList(),
            FaultPagination = new PaginationViewModel
            {
                Controller = "Machines", Action = "Details", Page = faultPage,
                TotalCount = allFaults.Count,
                RouteValues = new Dictionary<string, string> { ["id"] = id }
            }
        });
    }

    private async Task<MachineListViewModel> BuildListAsync(
        string? search, string? status, string? criticality, string? source,
        string sortBy, bool descending, int page, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        var result = await management.GetAsync(new MachineManagementQuery
        {
            Search = search, Status = status, Criticality = criticality, SourceSystem = source,
            SortBy = sortBy, Descending = descending, Page = page
        }, cancellationToken);
        var lookups = await management.GetLookupsAsync(cancellationToken);
        var routeValues = new Dictionary<string, string>
        {
            ["sortBy"] = sortBy, ["descending"] = descending.ToString().ToLowerInvariant()
        };
        AddRoute(routeValues, "search", search); AddRoute(routeValues, "status", status);
        AddRoute(routeValues, "criticality", criticality); AddRoute(routeValues, "source", source);
        return new MachineListViewModel
        {
            Machines = result.Items,
            Statuses = lookups.Statuses, Criticalities = lookups.Criticalities,
            Search = search, SelectedStatus = status, SelectedCriticality = criticality,
            SelectedSource = source, SortBy = sortBy, Descending = descending,
            UseErpAsMasterSource = lookups.UseErpAsMasterSource,
            Pagination = new PaginationViewModel
            {
                Controller = "Machines", Action = "Index", Page = result.Page,
                TotalCount = result.TotalCount, RouteValues = routeValues
            }
        };
    }

    private async Task<MachineFormViewModel> ToFormAsync(
        ManagedMachineDetailsViewModel details, CancellationToken cancellationToken) => new()
    {
        Id = details.Id, Code = details.Code, Name = details.Name,
        CompanyId = details.CompanyId, BranchId = details.BranchId,
        DepartmentId = details.DepartmentId, ProductionLineId = details.ProductionLineId,
        Model = details.Model, Age = details.Age, Criticality = details.Criticality,
        InstallationYear = details.InstallationYear, Status = details.Status,
        LastMaintenanceDate = details.LastMaintenanceAt?.LocalDateTime.Date,
        Lookups = await management.GetLookupsAsync(cancellationToken)
    };

    private async Task<IActionResult> InvalidFormAsync(MachineFormViewModel model, CancellationToken cancellationToken)
    {
        model.Lookups = await management.GetLookupsAsync(cancellationToken);
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return PartialView("_CreateOrEditModal", model);
    }

    private static void AddRoute(IDictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values[key] = value;
    }
}
