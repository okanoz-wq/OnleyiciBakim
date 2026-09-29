using Microsoft.AspNetCore.Mvc;
using OnleyiciBakim.Contracts.Dashboard;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("failure-analysis")]
    [ProducesResponseType<FailureAnalysisReport>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FailureAnalysisReport>> GetFailureAnalysis(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var range = GetRange(from, to);
        return Ok(await reportService.GetFailureAnalysisAsync(range.From, range.To, cancellationToken));
    }

    [HttpGet("maintenance-performance")]
    [ProducesResponseType<MaintenancePerformanceReport>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenancePerformanceReport>> GetMaintenancePerformance(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var range = GetRange(from, to);
        return Ok(await reportService.GetMaintenancePerformanceAsync(range.From, range.To, cancellationToken));
    }

    [HttpGet("model-rule-comparison")]
    [ProducesResponseType<ModelRuleComparisonReport>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelRuleComparisonReport>> GetModelRuleComparison(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var range = GetRange(from, to);
        return Ok(await reportService.GetModelRuleComparisonAsync(range.From, range.To, cancellationToken));
    }

    private static (DateTimeOffset From, DateTimeOffset To) GetRange(
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var end = to?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
        var start = from?.ToUniversalTime() ?? end.AddMonths(-6);
        if (start > end)
            throw new DomainValidationException("'from' tarihi 'to' tarihinden sonra olamaz.");
        return (start, end);
    }
}
