using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Contracts.Integrations;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Domain.Enums;

namespace OnleyiciBakim.Services;

public interface IRiskClassificationService
{
    RiskLevel Classify(decimal score);
    string GetLabel(RiskLevel level);
    string GetColorName(RiskLevel level);
    string GetColorHex(RiskLevel level);
}

public sealed record MaintenanceRecommendation(
    int RecommendedDays,
    string MaintenanceType,
    string Action,
    bool RequiresApproval);

public interface IMaintenanceRecommendationService
{
    MaintenanceRecommendation Get(RiskLevel level);
}

public interface IAlgorithmicRiskCalculator
{
    AlgorithmicCalculationResult Calculate(AlgorithmicRiskInput input);
}

public interface IHybridRiskCalculator
{
    decimal Calculate(decimal algorithmicScore, decimal machineLearningScore);
}

public sealed record AlgorithmicCalculationResult(
    decimal Score,
    AlgorithmicScoreBreakdown Breakdown,
    IReadOnlyList<string> Reasons,
    decimal DataCompletenessPercent,
    IReadOnlyList<string> MissingFactors,
    IReadOnlyList<RiskFactorResult> Factors);

public interface IRiskAssessmentService
{
    RiskAssessmentResponse Preview(RiskPreviewRequest request);
    Task<RiskAssessmentResponse> EvaluateMachineAsync(
        string machineId,
        EvaluateMachineRiskRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Teslim edilen yerel SQL Server şemasında analiz, güncel risk, uyarı ve planı
/// aynı transaction içinde kalıcılaştıran çalışma servisi.
/// </summary>
public interface ILocalRiskAssessmentService
{
    Task<RiskAssessmentResponse> EvaluateMachineAsync(
        string machineId,
        string calculationReason,
        CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(
        int months,
        int riskyMachineCount,
        int? year,
        CancellationToken cancellationToken);
}

public interface IReportService
{
    Task<FailureAnalysisReport> GetFailureAnalysisAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
    Task<MaintenancePerformanceReport> GetMaintenancePerformanceAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
    Task<ModelRuleComparisonReport> GetModelRuleComparisonAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
}

public interface IIntegrationService
{
    Task<IntegrationResult> SyncMachinesAsync(ErpMachineSyncRequest request, CancellationToken cancellationToken);
    Task<IntegrationResult> SyncFailuresAsync(ErpFailureSyncRequest request, CancellationToken cancellationToken);
}
