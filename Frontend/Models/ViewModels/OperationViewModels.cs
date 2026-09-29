using System.ComponentModel.DataAnnotations;
using OnleyiciBakimSistemi.Models;

namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed class OperationsIndexViewModel
{
    public string Module { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionLabel { get; init; }
    public IReadOnlyList<Machine> Machines { get; init; } = [];
    public IReadOnlyList<FaultRecord> Faults { get; init; } = [];
    public IReadOnlyList<MaintenancePlanItem> Plans { get; init; } = [];
    public OperationalReportViewModel? Report { get; init; }
}

public sealed class OperationalReportViewModel
{
    public IReadOnlyDictionary<string, string> Metrics { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows { get; init; } = [];
}

public sealed class OperationFormViewModel
{
    public string Module { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<Machine> Machines { get; set; } = [];
    public IReadOnlyList<FaultRecord> Faults { get; set; } = [];
    public IReadOnlyList<MaintenancePlanItem> Plans { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Personnel { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Counters { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Shifts { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> FailureCodes { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Components { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Checklists { get; set; } = [];
    public IReadOnlyList<OperationChecklistViewModel> ChecklistDefinitions { get; set; } = [];
    public List<OperationChecklistResultViewModel> ChecklistResults { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Companies { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Branches { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Departments { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> WorkCenters { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Warnings { get; set; } = [];
    public IReadOnlyList<DefinitionRowViewModel> Parts { get; set; } = [];
    public string? MachineId { get; set; }
    public string? FailureId { get; set; }
    public string? PlanId { get; set; }
    public string? WarningId { get; set; }
    public int? NumericId { get; set; }
    public int? CounterDefinitionId { get; set; }
    public int? ShiftId { get; set; }
    public int? FailureCodeId { get; set; }
    public int? ChecklistId { get; set; }
    public decimal? StartValue { get; set; }
    public decimal? EndValue { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Cost { get; set; }
    public int? Minutes { get; set; }
    public DateOnly? Date { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? CompanyId { get; set; }
    public string? BranchId { get; set; }
    public string? DepartmentId { get; set; }
    public string? WorkCenterId { get; set; }
    public string? PersonnelId { get; set; }
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? ComponentCode { get; set; }
    public string? PartCode { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
}

public sealed class OperationChecklistViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<OperationChecklistItemViewModel> Items { get; set; } = [];
}

public sealed class OperationChecklistItemViewModel
{
    public int ItemId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool DescriptionRequired { get; set; }
}

public sealed class OperationChecklistResultViewModel
{
    public int ItemId { get; set; }
    public string? Result { get; set; }
    public string? Description { get; set; }
}
