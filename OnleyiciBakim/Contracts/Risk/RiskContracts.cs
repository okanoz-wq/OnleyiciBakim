using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;

namespace OnleyiciBakim.Contracts.Risk;

public sealed class AlgorithmicRiskInput
{
    [Range(0, 36500)]
    public int? DaysSinceLastMaintenance { get; init; }

    [Range(0, 1000)]
    public int? FailureCount30Days { get; init; }

    [Range(0, 1000)]
    public int? ErrorCount7Days { get; init; }

    [Range(0, 100)]
    public decimal? AnomalyScore7Days { get; init; }

    [Range(0, 100)]
    public decimal? ProductionIntensity7Days { get; init; }

    [Range(0, 100000)]
    public decimal? TotalDowntimeHours30Days { get; init; }

    public bool? RepeatedFailure { get; init; }

    [Range(0, 1000)]
    public int? RepeatedFailureCount90Days { get; init; }
    public string? Criticality { get; init; }

    [Range(0, 10000)]
    public decimal? CounterLimitOverrunPercent { get; init; }

    [Range(0, 1000)]
    public int? ProductionStoppingFailureCount30Days { get; init; }
}

public sealed class RiskPreviewRequest
{
    [Range(0, 100)]
    public decimal MachineLearningScore { get; init; }

    public AlgorithmicRiskInput AlgorithmicInput { get; init; } = new();
}

public class EvaluateMachineRiskRequest
{
    [Range(0, 100)]
    public decimal? MachineLearningScore { get; init; }

    [Range(0, 100)]
    public decimal? ModelConfidence { get; init; }

    [StringLength(100)]
    public string? ModelName { get; init; }

    [StringLength(100)]
    public string? ModelVersion { get; init; }

    [StringLength(100)]
    public string? CorrelationId { get; init; }

    public IReadOnlyList<string>? MachineLearningReasons { get; init; }

    /// <summary>Yalnızca Development mock istemcisinde: success, timeout, unavailable, invalid-score.</summary>
    public string? MockScenario { get; init; }
}

public sealed record AlgorithmicScoreBreakdown(
    decimal FailureFrequency,
    decimal MaintenanceAge,
    decimal Downtime,
    decimal RepeatedFailure,
    decimal TelemetryAnomaly,
    decimal Criticality,
    decimal Total);

public sealed record RiskFactorResult(
    string Key,
    string DisplayName,
    string? RawValue,
    decimal NormalizedValue,
    decimal Weight,
    decimal Contribution,
    bool IsAvailable,
    string Description);

public sealed record RiskAssessmentResponse(
    string? Id,
    string? MachineId,
    DateTimeOffset EvaluatedAt,
    decimal AlgorithmicScore,
    decimal? MachineLearningScore,
    decimal AlgorithmWeight,
    decimal MachineLearningWeight,
    RiskPresentation HybridRisk,
    AlgorithmicScoreBreakdown Breakdown,
    IReadOnlyList<string> Reasons,
    string RecommendedAction,
    string? ModelName,
    string? ModelVersion,
    decimal? ModelConfidence,
    string? CorrelationId,
    decimal DataCompletenessPercent,
    IReadOnlyList<string> MissingFactors,
    IReadOnlyList<RiskFactorResult> Factors,
    bool IsFallback,
    string MlStatus,
    IReadOnlyList<string> AppliedOverrides,
    IReadOnlyDictionary<string, decimal> CalculationDetails);

public sealed class RiskHistoryQuery : PaginationQuery
{
    public string? MachineId { get; init; }
    public string? RiskLevel { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed record RiskHistoryItemResponse(
    string Id,
    string MachineId,
    string MachineName,
    DateTimeOffset EvaluatedAt,
    decimal AlgorithmicScore,
    decimal? MachineLearningScore,
    RiskPresentation HybridRisk,
    IReadOnlyList<string> Reasons,
    string RecommendedAction,
    string? ModelName,
    string? ModelVersion,
    decimal? ModelConfidence,
    string? CorrelationId);

public sealed record AlertResponse(
    string Id,
    DateTimeOffset GeneratedAt,
    string MachineId,
    string MachineName,
    string RiskAssessmentId,
    RiskPresentation Risk,
    string Title,
    string Description,
    string RecommendedAction,
    string Status,
    DateTimeOffset? DueAt,
    string? ResponsiblePersonnel);

public sealed class ResolveAlertRequest
{
    [Required]
    public string ResolutionNote { get; init; } = null!;
}
