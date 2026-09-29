using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Domain.Entities;

public sealed class FailureRecord : BaseEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public string MachineId { get; set; } = null!;
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string FailureType { get; set; } = null!;
    public string Severity { get; set; } = "Orta";
    public bool IsRecurring { get; set; }
    public bool IsPlannedDowntime { get; set; }
    public int? FirstResponseMinutes { get; set; }
    public int? InterventionMinutes { get; set; }
    public int? DowntimeMinutes { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Open;
    public string? RootCause { get; set; }
    public string? Description { get; set; }
    public string? PersonnelId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public decimal? EstimatedCost { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Resolution { get; set; }
    public string? DataSource { get; set; }
    public Machine Machine { get; set; } = null!;
    public Personnel? Personnel { get; set; }
    public ICollection<FailureIntervention> Interventions { get; set; } = [];
}

public sealed class FailureIntervention : BaseEntity
{
    public string Id { get; set; } = null!;
    public string FailureRecordId { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? PersonnelId { get; set; }
    public string Note { get; set; } = null!;
    public FailureRecord FailureRecord { get; set; } = null!;
    public Personnel? Personnel { get; set; }
}

public sealed class MaintenanceRecord : BaseEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset PerformedAt { get; set; }
    public string MachineId { get; set; } = null!;
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string MaintenanceType { get; set; } = null!;
    public string? Reason { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "Tamamlandı";
    public string? Result { get; set; }
    public string? PersonnelId { get; set; }
    public DateTimeOffset? NextMaintenanceAt { get; set; }
    public decimal? Cost { get; set; }
    public string? FailureRecordId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? ChecklistResultJson { get; set; }
    public string? DataSource { get; set; }
    public Machine Machine { get; set; } = null!;
    public Personnel? Personnel { get; set; }
    public FailureRecord? FailureRecord { get; set; }
}

public sealed class ErrorRecord : BaseEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public string MachineId { get; set; } = null!;
    public string ErrorCode { get; set; } = null!;
    public string? Message { get; set; }
    public string Severity { get; set; } = "Orta";
    public string Status { get; set; } = "Açık";
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? FailureRecordId { get; set; }
    public bool IsAutomaticAlert { get; set; }
    public string? DataSource { get; set; }
    public Machine Machine { get; set; } = null!;
    public FailureRecord? FailureRecord { get; set; }
}

public sealed class MaintenancePlan : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MachineId { get; set; } = null!;
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public DateTimeOffset PlannedAt { get; set; }
    public string MaintenanceType { get; set; } = "Önleyici";
    public string Priority { get; set; } = "Orta";
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? ResponsiblePersonnelId { get; set; }
    public MaintenancePlanStatus Status { get; set; } = MaintenancePlanStatus.Planned;
    public string? Justification { get; set; }
    public decimal? ModelConfidence { get; set; }
    public string? Recommendation { get; set; }
    public string? RiskAssessmentId { get; set; }
    public string? ErpWorkOrderNumber { get; set; }
    public string? DataSource { get; set; }
    public Machine Machine { get; set; } = null!;
    public Personnel? ResponsiblePersonnel { get; set; }
    public RiskAssessment? RiskAssessment { get; set; }
    public ICollection<MaintenancePlanTask> Tasks { get; set; } = [];
    public ICollection<MaintenanceExecution> Executions { get; set; } = [];
}

public sealed class MaintenancePlanTask : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MaintenancePlanId { get; set; } = null!;
    public int Order { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ResponsiblePersonnelId { get; set; }
    public bool IsCompleted { get; set; }
    public MaintenancePlan MaintenancePlan { get; set; } = null!;
    public Personnel? ResponsiblePersonnel { get; set; }
}

public sealed class MaintenanceExecution : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MaintenancePlanId { get; set; } = null!;
    public MaintenanceExecutionStatus Status { get; set; } = MaintenanceExecutionStatus.NotStarted;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? PersonnelId { get; set; }
    public string? Result { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal? Cost { get; set; }
    public MaintenancePlan MaintenancePlan { get; set; } = null!;
    public Personnel? Personnel { get; set; }
    public ICollection<MaintenanceExecutionChecklistResult> ChecklistResults { get; set; } = [];
    public ICollection<ReplacedPart> ReplacedParts { get; set; } = [];
}

public sealed class MaintenanceExecutionChecklistResult : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MaintenanceExecutionId { get; set; } = null!;
    public string ChecklistItemId { get; set; } = null!;
    public bool IsPassed { get; set; }
    public string? Note { get; set; }
    public MaintenanceExecution MaintenanceExecution { get; set; } = null!;
}

public sealed class ReplacedPart : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MaintenanceExecutionId { get; set; } = null!;
    public string PartCode { get; set; } = null!;
    public string PartName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? ErpStockMovementId { get; set; }
    public MaintenanceExecution MaintenanceExecution { get; set; } = null!;
}
