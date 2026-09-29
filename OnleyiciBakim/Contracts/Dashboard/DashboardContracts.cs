using OnleyiciBakim.Contracts.Common;

namespace OnleyiciBakim.Contracts.Dashboard;

public sealed record DashboardResponse(
    int TotalMachines,
    int HighOrCriticalRiskMachines,
    int PendingMaintenancePlans,
    decimal AverageFailureDurationHours,
    IReadOnlyList<MonthlyFailurePoint> MonthlyFailureAnalysis,
    IReadOnlyList<CategoryCount> FailureTypeDistribution,
    IReadOnlyList<CategoryCount> RiskDistribution,
    IReadOnlyList<RiskiestMachineResponse> RiskiestMachines,
    DateTimeOffset GeneratedAt);

public sealed record MonthlyFailurePoint(int Year, int Month, string Label, int FailureCount);
public sealed record CategoryCount(string Category, int Count);
public sealed record RiskiestMachineResponse(
    string Id,
    string Code,
    string Name,
    string? Location,
    DateTimeOffset? NextMaintenanceAt,
    RiskPresentation Risk);

public sealed record FailureAnalysisReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int FailureCount,
    decimal TotalDowntimeHours,
    decimal AverageInterventionMinutes,
    decimal TotalEstimatedCost,
    IReadOnlyList<CategoryCount> ByType,
    IReadOnlyList<CategoryCount> BySeverity);

public sealed record MaintenancePerformanceReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int CompletedCount,
    int PlannedCount,
    decimal CompletionRatePercent,
    decimal TotalCost,
    decimal AverageDurationMinutes,
    int OverdueCount);

public sealed record ModelRuleComparisonItem(
    string RiskAssessmentId,
    string MachineId,
    DateTimeOffset EvaluatedAt,
    decimal AlgorithmicScore,
    decimal MachineLearningScore,
    decimal HybridScore,
    decimal AbsoluteDifference);

public sealed record ModelRuleComparisonReport(
    int SampleCount,
    decimal MeanAbsoluteDifference,
    decimal AverageAlgorithmicScore,
    decimal AverageMachineLearningScore,
    decimal AverageHybridScore,
    IReadOnlyList<ModelRuleComparisonItem> LargestDifferences);
