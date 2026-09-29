using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Integrations.Ml;
using OnleyiciBakim.Services;
using OnleyiciBakim.Options;
using OnleyiciBakim.Models.Ai;
using Xunit;

namespace OnleyiciBakim.Tests;

public sealed class WorkflowAndIntegrationTests
{
    private readonly WorkflowTransitionService _transitions = new();

    [Fact]
    public void FailureWorkflow_RejectsClosingAnOpenFailure()
    {
        Assert.Throws<ConflictException>(() =>
            _transitions.EnsureFailureTransition(RecordStatus.Open, RecordStatus.Closed));
    }

    [Fact]
    public void MaintenanceWorkflow_AllowsRequiredHappyPath()
    {
        _transitions.EnsureMaintenancePlanTransition(
            MaintenancePlanStatus.Draft, MaintenancePlanStatus.PendingApproval);
        _transitions.EnsureMaintenancePlanTransition(
            MaintenancePlanStatus.PendingApproval, MaintenancePlanStatus.Approved);
        _transitions.EnsureMaintenancePlanTransition(
            MaintenancePlanStatus.Approved, MaintenancePlanStatus.Planned);
        _transitions.EnsureMaintenancePlanTransition(
            MaintenancePlanStatus.Planned, MaintenancePlanStatus.InProgress);
        _transitions.EnsureMaintenancePlanTransition(
            MaintenancePlanStatus.InProgress, MaintenancePlanStatus.Completed);
    }

    [Theory]
    [InlineData("timeout", "timeout")]
    [InlineData("unavailable", "unavailable")]
    [InlineData("invalid-score", "invalid-score")]
    public async Task MockMlClient_ExposesFailureScenario(string scenario, string errorCode)
    {
        var client = new MockMlPredictionClient();
        var result = await client.PredictAsync(
            new MlPredictionRequest("M-1", new MachineFeatureDto(), scenario),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Null(result.RiskScore);
    }

    [Theory]
    [InlineData(RiskLevel.Low, 60, "Periyodik")]
    [InlineData(RiskLevel.Medium, 21, "Planlı")]
    [InlineData(RiskLevel.High, 7, "Acil")]
    [InlineData(RiskLevel.Critical, 3, "Acil müdahale")]
    public void MaintenanceRecommendation_UsesCentralConfiguration(
        RiskLevel level,
        int days,
        string type)
    {
        var service = new MaintenanceRecommendationService(
            Microsoft.Extensions.Options.Options.Create(new MaintenanceRecommendationOptions()));
        var result = service.Get(level);
        Assert.Equal(days, result.RecommendedDays);
        Assert.Equal(type, result.MaintenanceType);
    }
}
