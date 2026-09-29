using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Domain.Entities;

public sealed class RiskAssessment : BaseEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset EvaluatedAt { get; set; }
    public string MachineId { get; set; } = null!;
    public decimal AlgorithmicScore { get; set; }
    public decimal? MachineLearningScore { get; set; }
    public decimal HybridScore { get; set; }
    public decimal AlgorithmWeight { get; set; }
    public decimal MachineLearningWeight { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string RiskReasons { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string? AlgorithmDetailsJson { get; set; }
    public string? ModelName { get; set; }
    public string? ModelVersion { get; set; }
    public decimal? ModelConfidence { get; set; }
    public string? CorrelationId { get; set; }
    public decimal DataCompletenessPercent { get; set; }
    public bool IsFallback { get; set; }
    public string MlStatus { get; set; } = "Success";
    public string RuleSetVersion { get; set; } = "algorithmic-v1";
    public string Status { get; set; } = "Aktif";
    public string DataSource { get; set; } = "Hibrit";
    public Machine Machine { get; set; } = null!;
    public ICollection<Alert> Alerts { get; set; } = [];
    public ICollection<RiskFactorResultEntity> Factors { get; set; } = [];
    public ICollection<MlPredictionRecord> MlPredictions { get; set; } = [];
}

public sealed class RiskFactorResultEntity : BaseEntity
{
    public long Id { get; set; }
    public string RiskAssessmentId { get; set; } = null!;
    public string FactorKey { get; set; } = null!;
    public string? RawValue { get; set; }
    public decimal NormalizedValue { get; set; }
    public decimal Weight { get; set; }
    public decimal Contribution { get; set; }
    public bool IsAvailable { get; set; }
    public string Description { get; set; } = null!;
    public RiskAssessment RiskAssessment { get; set; } = null!;
}

public sealed class MlPredictionRecord : BaseEntity
{
    public string Id { get; set; } = null!;
    public string RiskAssessmentId { get; set; } = null!;
    public string? PredictionId { get; set; }
    public decimal? RiskScore { get; set; }
    public string Status { get; set; } = null!;
    public string? ModelName { get; set; }
    public string? ModelVersion { get; set; }
    public decimal? Confidence { get; set; }
    public DateTimeOffset? PredictedAtUtc { get; set; }
    public string? WarningsJson { get; set; }
    public string? ErrorCode { get; set; }
    public RiskAssessment RiskAssessment { get; set; } = null!;
}

public sealed class DecisionHistory : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MachineId { get; set; } = null!;
    public string? RiskAssessmentId { get; set; }
    public DecisionType DecisionType { get; set; }
    public decimal? PreviousScore { get; set; }
    public decimal FinalScore { get; set; }
    public string? Reason { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset DecidedAtUtc { get; set; }
    public string? ModelVersion { get; set; }
    public string? RuleSetVersion { get; set; }
}

public sealed class Alert : BaseEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; }
    public string MachineId { get; set; } = null!;
    public string RiskAssessmentId { get; set; } = null!;
    public RiskLevel Level { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string RecommendedAction { get; set; } = null!;
    public AlertStatus Status { get; set; } = AlertStatus.Open;
    public DateTimeOffset? DueAt { get; set; }
    public string? ResponsiblePersonnelId { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public Machine Machine { get; set; } = null!;
    public RiskAssessment RiskAssessment { get; set; } = null!;
    public Personnel? ResponsiblePersonnel { get; set; }
}

public sealed class IntegrationSyncLog : BaseEntity
{
    public long Id { get; set; }
    public string SystemName { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? ExternalId { get; set; }
    public SyncDirection Direction { get; set; }
    public SyncStatus Status { get; set; }
    public string CorrelationId { get; set; } = null!;
    public string? PayloadJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? OperationType { get; set; }
}

public sealed class ErpWorkOrderMapping : BaseEntity
{
    public string Id { get; set; } = null!;
    public string MaintenancePlanId { get; set; } = null!;
    public string ErpWorkOrderId { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string Status { get; set; } = null!;
}

public sealed class OutboxMessage : BaseEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
}

public sealed class AuditLog : BaseEntity
{
    public long Id { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? UserId { get; set; }
    public string? ChangesJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
