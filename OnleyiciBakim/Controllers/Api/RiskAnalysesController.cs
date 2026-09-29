using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/risk-analyses")]
public sealed class RiskAnalysesController(
    ApplicationDbContext dbContext,
    IRiskAssessmentService riskAssessmentService,
    ILocalRiskAssessmentService? localRiskAssessmentService = null) : ControllerBase
{
    [HttpPost("machines/{machineId}")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RiskAssessmentResponse>> Analyze(
        string machineId,
        EvaluateMachineRiskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await riskAssessmentService.EvaluateMachineAsync(
            machineId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("local-machines/{machineId}")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RiskAssessmentResponse>> AnalyzeLocalMachine(
        string machineId,
        [FromQuery] string reason = "Yönetici yeniden hesaplama isteği",
        CancellationToken cancellationToken = default)
    {
        if (localRiskAssessmentService is null)
            throw new DomainValidationException("Yerel SQL Server risk servisi etkin değildir.");
        var result = await localRiskAssessmentService.EvaluateMachineAsync(
            machineId, reason, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<RiskAssessmentResponse>>> Bulk(
        BulkRiskAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MachineIds.Count > 100)
            throw new DomainValidationException("Tek toplu istekte en fazla 100 makine analiz edilebilir.");
        var results = new List<RiskAssessmentResponse>(request.MachineIds.Count);
        foreach (var machineId in request.MachineIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            results.Add(await riskAssessmentService.EvaluateMachineAsync(
                machineId, new EvaluateMachineRiskRequest(), cancellationToken));
        }
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var result = await dbContext.RiskAssessments.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.MachineId, x.EvaluatedAt, x.AlgorithmicScore,
                x.MachineLearningScore, x.HybridScore, x.RiskLevel,
                x.DataCompletenessPercent, x.IsFallback, x.MlStatus,
                x.ModelName, x.ModelVersion, x.RuleSetVersion,
                Factors = x.Factors.OrderBy(f => f.Id).Select(f => new
                {
                    f.FactorKey, f.RawValue, f.NormalizedValue, f.Weight,
                    f.Contribution, f.IsAvailable, f.Description
                })
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli risk analizi bulunamadı.");
        return Ok(result);
    }
}

public sealed class BulkRiskAnalysisRequest
{
    [Required, MinLength(1)]
    public IReadOnlyList<string> MachineIds { get; init; } = [];
}
