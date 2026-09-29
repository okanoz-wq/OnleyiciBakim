using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakim.Contracts.Management;

public sealed class MeterReadingRequest
{
    [Required] public string MachineId { get; set; } = string.Empty;
    public int CounterDefinitionId { get; set; }
    public DateOnly Date { get; set; }
    public int ShiftId { get; set; }
    [Range(0, double.MaxValue)] public decimal StartValue { get; set; }
    [Range(0, double.MaxValue)] public decimal EndValue { get; set; }
    [Range(0, int.MaxValue)] public int? WorkingMinutes { get; set; }
    [Range(0, int.MaxValue)] public int? IdleMinutes { get; set; }
    [Range(0, int.MaxValue)] public int? ProductionMinutes { get; set; }
    [Range(0, double.MaxValue)] public decimal? ProductionQuantity { get; set; }
    public string? Description { get; set; }
}

public sealed class WorkCalendarRequest
{
    [Required] public string MachineId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int ShiftId { get; set; }
    [Range(0, 1440)] public int PlannedWorkingMinutes { get; set; }
    [Range(0, double.MaxValue)] public decimal? PlannedProductionQuantity { get; set; }
    public string Status { get; set; } = "Planlandı";
    public string? Description { get; set; }
}

public sealed class MachineTransferRequest
{
    [Required] public string MachineId { get; set; } = string.Empty;
    [Required] public string CompanyId { get; set; } = string.Empty;
    [Required] public string BranchId { get; set; } = string.Empty;
    [Required] public string DepartmentId { get; set; } = string.Empty;
    [Required] public string WorkCenterId { get; set; } = string.Empty;
    public DateTime TransferAt { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; } = string.Empty;
    public string? PersonnelId { get; set; }
}

public sealed class FailureReportRequest
{
    [Required] public string MachineId { get; set; } = string.Empty;
    public int FailureCodeId { get; set; }
    public int? ShiftId { get; set; }
    public string? ComponentCode { get; set; }
    public DateTime StartedAt { get; set; }
    public string? WorkOrderNo { get; set; }
    public string? ProductCode { get; set; }
    public string? Severity { get; set; }
    public bool ProductionStopped { get; set; }
    public string? FirstSymptom { get; set; }
    [Range(0, int.MaxValue)] public int? EstimatedDowntimeMinutes { get; set; }
    public string? ReporterPersonnelId { get; set; }
    public string? Description { get; set; }
}

public sealed class FailureInterventionRequest
{
    [Required] public string FailureId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? PersonnelId { get; set; }
    [Required] public string Action { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public string? Result { get; set; }
    [Range(0, int.MaxValue)] public int? ActualDowntimeMinutes { get; set; }
    public string? Description { get; set; }
    public List<ChangedPartRequest> Parts { get; set; } = [];
}

public sealed class ChangedPartRequest
{
    public int? PartId { get; set; }
    [Required] public string PartCode { get; set; } = string.Empty;
    [Range(0.001, double.MaxValue)] public decimal Quantity { get; set; }
    public string? Description { get; set; }
}

public sealed class FailureCloseRequest
{
    [Required] public string FailureId { get; set; } = string.Empty;
    public DateTime ClosedAt { get; set; }
    public DateTime FailureEndedAt { get; set; }
    [Required] public string ClosingPersonnelId { get; set; } = string.Empty;
    [Required] public string Resolution { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal? ProductionLoss { get; set; }
    public string? ProductionLossUnit { get; set; }
    public string FinalStatus { get; set; } = "Kapandı";
}

public sealed class MaintenanceExecutionRequest
{
    [Required] public string PlanId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string? PersonnelId { get; set; }
    public string? Result { get; set; }
    public string? MachineStatusAfter { get; set; }
    public int? ChecklistId { get; set; }
    [Range(0, double.MaxValue)] public decimal? Cost { get; set; }
    public string? Description { get; set; }
    public List<ChecklistResultRequest> ChecklistResults { get; set; } = [];
}

public sealed class ChecklistResultRequest
{
    public int ItemId { get; set; }
    public string? Result { get; set; }
    public string? Description { get; set; }
}

public sealed class WorkOrderRequest
{
    public string? PlanId { get; set; }
    public string? FailureId { get; set; }
    public string? WarningId { get; set; }
    [Required] public string MachineId { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public string? ResponsiblePersonnelId { get; set; }
    public string? Description { get; set; }
}

public sealed record StatusChangeRequest([Required] string Status, string? PersonnelId, string? Description);
public sealed record WarningActionRequest([Required] string Action, string? PersonnelId, string? Reason, DateOnly? PlannedDate);
public sealed record WorkflowResult(string Id, string Message, string? MachineId = null);
