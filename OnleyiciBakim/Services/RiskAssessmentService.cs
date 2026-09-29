using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Entities;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Models.Ai;
using OnleyiciBakim.Options;
using OnleyiciBakim.Services.Features;
using OnleyiciBakim.Services.Risk;

namespace OnleyiciBakim.Services;

public sealed class RiskAssessmentService(
    ApplicationDbContext dbContext,
    IAlgorithmicRiskCalculator algorithmicRiskCalculator,
    IHybridRiskCalculator hybridRiskCalculator,
    IRuleBasedRiskService ruleBasedRiskService,
    IHybridRiskService hybridRiskService,
    IMachineFeatureService machineFeatureService,
    IRiskClassificationService classificationService,
    IMaintenanceRecommendationService recommendationService,
    IMlPredictionClient mlPredictionClient,
    IOptions<HybridRiskOptions> hybridOptions,
    IOptions<AlgorithmicRiskOptions> algorithmOptions,
    IOptions<IntegrationOptions> integrationOptions,
    ILogger<RiskAssessmentService> logger)
    : IRiskAssessmentService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
        MachineLocks = new(StringComparer.OrdinalIgnoreCase);

    public RiskAssessmentResponse Preview(RiskPreviewRequest request)
    {
        var calculation = algorithmicRiskCalculator.Calculate(request.AlgorithmicInput);
        var prediction = new MlPredictionResult(
            true, request.MachineLearningScore, "ManualPreview", null, null,
            DateTimeOffset.UtcNow, null, [], "Provided");
        var score = hybridRiskCalculator.Calculate(calculation.Score, request.MachineLearningScore);
        var level = classificationService.Classify(score);
        return new RiskAssessmentResponse(
            null, null, DateTimeOffset.UtcNow, calculation.Score, request.MachineLearningScore,
            hybridOptions.Value.AlgorithmWeight, hybridOptions.Value.MachineLearningWeight,
            ToPresentation(score, level), calculation.Breakdown, calculation.Reasons,
            recommendationService.Get(level).Action, prediction.ModelName,
            prediction.ModelVersion, prediction.Confidence, null,
            calculation.DataCompletenessPercent, calculation.MissingFactors,
            calculation.Factors, false, prediction.Status, [],
            new Dictionary<string, decimal> { ["weightedScoreBeforeOverrides"] = score });
    }

    public async Task<RiskAssessmentResponse> EvaluateMachineAsync(
        string machineId,
        EvaluateMachineRiskRequest request,
        CancellationToken cancellationToken)
    {
        var gate = MachineLocks.GetOrAdd(machineId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await EvaluateMachineCoreAsync(machineId, request, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<RiskAssessmentResponse> EvaluateMachineCoreAsync(
        string machineId,
        EvaluateMachineRiskRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            var existing = await dbContext.RiskAssessments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CorrelationId == request.CorrelationId, cancellationToken);
            if (existing is not null)
                return MapExisting(existing);
        }

        var machine = await dbContext.Machines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == machineId, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{machineId}' kimlikli makine bulunamadı.");

        var evaluatedAt = DateTimeOffset.UtcNow;
        var features = await machineFeatureService.BuildFeaturesAsync(
            machineId, evaluatedAt.UtcDateTime, cancellationToken);
        var calculation = ruleBasedRiskService.Calculate(features);

        MlPredictionResult prediction;
        if (request.MachineLearningScore.HasValue)
        {
            prediction = new MlPredictionResult(
                true, request.MachineLearningScore, request.ModelName, request.ModelVersion,
                request.CorrelationId, evaluatedAt, request.ModelConfidence,
                request.MachineLearningReasons ?? [], "Provided",
                DataCompletenessRate: features.DataCompletenessRate);
        }
        else if (features.DataCompletenessRate < hybridOptions.Value.MinimumCompletenessThreshold)
        {
            prediction = new MlPredictionResult(
                false, null, null, null, null, null, null, [], "Skipped",
                "INSUFFICIENT_DATA", "ML tahmini için veri tamlığı %50'nin altında.",
                DataCompletenessRate: features.DataCompletenessRate);
        }
        else
        {
            prediction = await mlPredictionClient.PredictAsync(
                new MlPredictionRequest(machineId, features, request.MockScenario),
                cancellationToken);
        }

        if (!prediction.IsSuccess)
        {
            logger.LogWarning(
                "ML analizi başarısız. MachineId={MachineId} ErrorCode={ErrorCode} Policy={Policy}",
                machineId, prediction.ErrorCode, hybridOptions.Value.MlFailurePolicy);
        }

        var hybrid = hybridRiskService.Calculate(
            calculation.Score,
            prediction.IsSuccess ? prediction.RiskScore : null,
            features,
            evaluatedAt);
        var isFallback = hybrid.AiWeight == 0m;
        var response = CreateResponse(
            null, machineId, evaluatedAt, calculation, prediction, request.CorrelationId,
            isFallback, hybrid, features);

        IDbContextTransaction? transaction = null;
        if (dbContext.Database.IsRelational())
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assessment = new RiskAssessment
            {
                Id = NewId("RSK"),
                MachineId = machineId,
                EvaluatedAt = evaluatedAt,
                AlgorithmicScore = response.AlgorithmicScore,
                MachineLearningScore = response.MachineLearningScore,
                HybridScore = response.HybridRisk.Score,
                AlgorithmWeight = response.AlgorithmWeight,
                MachineLearningWeight = response.MachineLearningWeight,
                RiskLevel = classificationService.Classify(response.HybridRisk.Score),
                RiskReasons = string.Join("; ", response.Reasons),
                RecommendedAction = response.RecommendedAction,
                AlgorithmDetailsJson = JsonSerializer.Serialize(new CalculationSnapshot(
                    response.Breakdown, response.Factors, response.MissingFactors,
                    features, response.AppliedOverrides, response.CalculationDetails)),
                ModelName = response.ModelName ?? integrationOptions.Value.MachineLearningModelName,
                ModelVersion = response.ModelVersion,
                ModelConfidence = response.ModelConfidence,
                CorrelationId = response.CorrelationId,
                DataCompletenessPercent = response.DataCompletenessPercent,
                IsFallback = response.IsFallback,
                MlStatus = response.MlStatus,
                RuleSetVersion = algorithmOptions.Value.RuleSetVersion,
                DataSource = response.IsFallback ? "RuleOnlyFallback" :
                    $"Kural %{response.AlgorithmWeight * 100:0} + AI %{response.MachineLearningWeight * 100:0}"
            };
            dbContext.RiskAssessments.Add(assessment);
            dbContext.RiskFactorResults.AddRange(response.Factors.Select(factor =>
                new RiskFactorResultEntity
                {
                    RiskAssessmentId = assessment.Id,
                    FactorKey = factor.Key,
                    RawValue = factor.RawValue,
                    NormalizedValue = factor.NormalizedValue,
                    Weight = factor.Weight,
                    Contribution = factor.Contribution,
                    IsAvailable = factor.IsAvailable,
                    Description = factor.Description
                }));
            dbContext.MlPredictionRecords.Add(new MlPredictionRecord
            {
                Id = NewId("MLP"),
                RiskAssessmentId = assessment.Id,
                PredictionId = prediction.PredictionId,
                RiskScore = prediction.RiskScore,
                Status = prediction.Status,
                ModelName = prediction.ModelName,
                ModelVersion = prediction.ModelVersion,
                Confidence = prediction.Confidence,
                PredictedAtUtc = prediction.PredictedAtUtc,
                WarningsJson = JsonSerializer.Serialize(prediction.Warnings),
                ErrorCode = prediction.ErrorCode
            });
            dbContext.DecisionHistory.Add(new DecisionHistory
            {
                Id = NewId("DEC"),
                MachineId = machineId,
                RiskAssessmentId = assessment.Id,
                DecisionType = isFallback
                    ? DecisionType.AlgorithmOnlyFallback
                    : DecisionType.HybridAnalysis,
                FinalScore = assessment.HybridScore,
                Reason = string.Join("; ", response.Reasons),
                DecidedAtUtc = evaluatedAt,
                ModelVersion = assessment.ModelVersion,
                RuleSetVersion = assessment.RuleSetVersion
            });

            var trackedMachine = await dbContext.Machines.FirstAsync(
                x => x.Id == machineId, cancellationToken);
            trackedMachine.CurrentRiskScore = assessment.HybridScore;
            trackedMachine.CurrentRiskLevel = assessment.RiskLevel;

            await UpsertWarningAndMaintenancePlanAsync(
                trackedMachine, assessment, response.AppliedOverrides, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return response with { Id = assessment.Id, ModelName = assessment.ModelName };
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task UpsertWarningAndMaintenancePlanAsync(
        Machine machine,
        RiskAssessment assessment,
        IReadOnlyList<string> appliedOverrides,
        CancellationToken cancellationToken)
    {
        var now = assessment.EvaluatedAt;
        var recommendation = recommendationService.Get(assessment.RiskLevel);
        var dueAt = now.AddDays(recommendation.RecommendedDays);
        if (assessment.RiskLevel is RiskLevel.High or RiskLevel.Critical &&
            !await dbContext.Alerts.AnyAsync(
                x => x.RiskAssessmentId == assessment.Id, cancellationToken))
        {
            dbContext.Alerts.Add(new Alert
            {
                Id = NewId("UYR"),
                GeneratedAt = now,
                MachineId = machine.Id,
                RiskAssessmentId = assessment.Id,
                Level = assessment.RiskLevel,
                Title = $"{machine.Name} için {classificationService.GetLabel(assessment.RiskLevel).ToLowerInvariant()} risk uyarısı",
                Description = assessment.RiskReasons,
                RecommendedAction = assessment.RecommendedAction,
                DueAt = dueAt
            });
        }

        if (!hybridOptions.Value.AutoCreateMaintenanceDraft)
            return;

        var activePlan = await dbContext.MaintenancePlans.FirstOrDefaultAsync(x =>
            x.MachineId == machine.Id &&
            x.Status != MaintenancePlanStatus.Completed &&
            x.Status != MaintenancePlanStatus.Cancelled, cancellationToken);
        var justification = string.Join("; ", new[] { assessment.RiskReasons }
            .Concat(appliedOverrides).Where(x => !string.IsNullOrWhiteSpace(x)));
        if (activePlan is not null)
        {
            activePlan.PlannedAt = dueAt;
            activePlan.MaintenanceType = recommendation.MaintenanceType;
            activePlan.Priority = classificationService.GetLabel(assessment.RiskLevel);
            activePlan.Justification = justification;
            activePlan.Recommendation = assessment.RecommendedAction;
            activePlan.ModelConfidence = assessment.ModelConfidence;
            activePlan.RiskAssessmentId = assessment.Id;
            activePlan.DataSource = assessment.IsFallback
                ? "Kural tabanlı risk motoru"
                : "Hibrit risk motoru";
            return;
        }

        dbContext.MaintenancePlans.Add(new MaintenancePlan
        {
            Id = NewId("PLN"),
            MachineId = machine.Id,
            PlannedAt = dueAt,
            MaintenanceType = recommendation.MaintenanceType,
            Priority = classificationService.GetLabel(assessment.RiskLevel),
            Status = MaintenancePlanStatus.Suggested,
            Justification = justification,
            Recommendation = assessment.RecommendedAction,
            ModelConfidence = assessment.ModelConfidence,
            RiskAssessmentId = assessment.Id,
            DataSource = assessment.IsFallback
                ? "Kural tabanlı risk motoru"
                : "Hibrit risk motoru"
        });
    }

    private RiskAssessmentResponse CreateResponse(
        string? id,
        string? machineId,
        DateTimeOffset evaluatedAt,
        AlgorithmicCalculationResult calculation,
        MlPredictionResult prediction,
        string? correlationId,
        bool isFallback,
        HybridRiskCalculation hybrid,
        MachineFeatureDto features)
    {
        var level = classificationService.Classify(hybrid.FinalRiskScore);
        var predictionMessages = prediction.Warnings
            .Concat(string.IsNullOrWhiteSpace(prediction.ErrorMessage)
                ? []
                : new[] { prediction.ErrorMessage });
        var reasons = calculation.Reasons
            .Concat(predictionMessages.Where(x => !string.IsNullOrWhiteSpace(x)))
            .Concat(hybrid.AppliedOverrides)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new RiskAssessmentResponse(
            id, machineId, evaluatedAt, calculation.Score, prediction.RiskScore,
            hybrid.RuleWeight, hybrid.AiWeight,
            ToPresentation(hybrid.FinalRiskScore, level), calculation.Breakdown, reasons,
            recommendationService.Get(level).Action, prediction.ModelName,
            prediction.ModelVersion, prediction.Confidence, correlationId,
            features.DataCompletenessRate, calculation.MissingFactors,
            calculation.Factors, isFallback, prediction.Status,
            hybrid.AppliedOverrides, hybrid.CalculationDetails);
    }

    private RiskAssessmentResponse MapExisting(RiskAssessment assessment)
    {
        var snapshot = string.IsNullOrWhiteSpace(assessment.AlgorithmDetailsJson)
            ? null
            : JsonSerializer.Deserialize<CalculationSnapshot>(assessment.AlgorithmDetailsJson);
        var breakdown = snapshot?.Breakdown ??
            new AlgorithmicScoreBreakdown(0, 0, 0, 0, 0, 0, assessment.AlgorithmicScore);
        return new RiskAssessmentResponse(
            assessment.Id, assessment.MachineId, assessment.EvaluatedAt,
            assessment.AlgorithmicScore, assessment.MachineLearningScore,
            assessment.AlgorithmWeight, assessment.MachineLearningWeight,
            ToPresentation(assessment.HybridScore, assessment.RiskLevel), breakdown,
            SplitReasons(assessment.RiskReasons), assessment.RecommendedAction,
            assessment.ModelName, assessment.ModelVersion, assessment.ModelConfidence,
            assessment.CorrelationId, assessment.DataCompletenessPercent,
            snapshot?.MissingFactors ?? [], snapshot?.Factors ?? [],
            assessment.IsFallback, assessment.MlStatus,
            snapshot?.AppliedOverrides ?? [], snapshot?.CalculationDetails ??
            new Dictionary<string, decimal>());
    }

    private RiskPresentation ToPresentation(decimal score, RiskLevel level) =>
        new(score, classificationService.GetLabel(level), classificationService.GetColorName(level),
            classificationService.GetColorHex(level));

    private static string[] SplitReasons(string reasons) =>
        reasons.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string NewId(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

    private sealed record CalculationSnapshot(
        AlgorithmicScoreBreakdown Breakdown,
        IReadOnlyList<RiskFactorResult> Factors,
        IReadOnlyList<string> MissingFactors,
        MachineFeatureDto? Features = null,
        IReadOnlyList<string>? AppliedOverrides = null,
        IReadOnlyDictionary<string, decimal>? CalculationDetails = null);
}
