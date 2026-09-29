namespace OnleyiciBakim.Domain.Enums;

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RecordStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    Cancelled
}

public enum MaintenancePlanStatus
{
    Suggested,
    Draft,
    PendingApproval,
    Approved,
    Planned,
    InProgress,
    Completed,
    Cancelled,
    Overdue
}

public enum AlertStatus
{
    Open,
    Assigned,
    ConvertedToPlan,
    NoActionRequired,
    Closed,
    Acknowledged = Assigned,
    Resolved = Closed
}

public enum SyncDirection
{
    Inbound,
    Outbound
}

public enum SyncStatus
{
    Received,
    Pending,
    Processing,
    Processed,
    Failed,
    DeadLetter
}

public enum MaintenanceExecutionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Cancelled
}

public enum DecisionType
{
    HybridAnalysis,
    AlgorithmOnlyFallback,
    ManualOverride,
    MaintenancePlanCreated,
    NoActionRequired
}
