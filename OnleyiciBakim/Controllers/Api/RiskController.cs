using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Contracts.Risk;
using OnleyiciBakim.Data;
using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;
using OnleyiciBakim.Services;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/risks")]
public sealed class RiskController(
    ApplicationDbContext dbContext,
    IRiskAssessmentService riskAssessmentService,
    IRiskClassificationService classificationService)
    : ControllerBase
{
    [HttpPost("preview")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status200OK)]
    public ActionResult<RiskAssessmentResponse> Preview(RiskPreviewRequest request) =>
        Ok(riskAssessmentService.Preview(request));

    [HttpPost("machines/{machineId}/evaluate")]
    [ProducesResponseType<RiskAssessmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RiskAssessmentResponse>> EvaluateMachine(
        string machineId,
        EvaluateMachineRiskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await riskAssessmentService.EvaluateMachineAsync(machineId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<RiskHistoryItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskHistoryItemResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.RiskAssessments
            .AsNoTracking()
            .Include(x => x.Machine)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException($"'{id}' kimlikli risk değerlendirmesi bulunamadı.");
        return Ok(Map(row.Id, row.MachineId, row.Machine.Name, row.EvaluatedAt,
            row.AlgorithmicScore, row.MachineLearningScore, row.HybridScore, row.RiskLevel,
            row.RiskReasons, row.RecommendedAction, row.ModelName, row.ModelVersion,
            row.ModelConfidence, row.CorrelationId));
    }

    [HttpGet("history")]
    [ProducesResponseType<PagedResponse<RiskHistoryItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RiskHistoryItemResponse>>> GetHistory(
        [FromQuery] RiskHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RiskAssessments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MachineId))
            query = query.Where(x => x.MachineId == request.MachineId);
        if (TryParseRiskLevel(request.RiskLevel, out var riskLevel))
            query = query.Where(x => x.RiskLevel == riskLevel);
        if (request.From.HasValue)
            query = query.Where(x => x.EvaluatedAt >= request.From.Value.ToUniversalTime());
        if (request.To.HasValue)
            query = query.Where(x => x.EvaluatedAt <= request.To.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.EvaluatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,
                x.MachineId,
                MachineName = x.Machine.Name,
                x.EvaluatedAt,
                x.AlgorithmicScore,
                x.MachineLearningScore,
                x.HybridScore,
                x.RiskLevel,
                x.RiskReasons,
                x.RecommendedAction,
                x.ModelName,
                x.ModelVersion,
                x.ModelConfidence,
                x.CorrelationId
            })
            .ToListAsync(cancellationToken);
        var items = rows.Select(x => Map(
            x.Id, x.MachineId, x.MachineName, x.EvaluatedAt, x.AlgorithmicScore,
            x.MachineLearningScore, x.HybridScore, x.RiskLevel, x.RiskReasons,
            x.RecommendedAction, x.ModelName, x.ModelVersion, x.ModelConfidence, x.CorrelationId))
            .ToArray();
        return Ok(new PagedResponse<RiskHistoryItemResponse>(
            items, request.Page, request.PageSize, total));
    }

    [HttpGet("current")]
    [ProducesResponseType<IReadOnlyList<RiskHistoryItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RiskHistoryItemResponse>>> GetCurrent(
        [FromQuery] decimal? minimumScore,
        CancellationToken cancellationToken)
    {
        var machines = dbContext.Machines.AsNoTracking();
        if (minimumScore.HasValue)
            machines = machines.Where(x => x.CurrentRiskScore >= minimumScore.Value);

        var rows = await machines
            .OrderByDescending(x => x.CurrentRiskScore)
            .Select(x => new
            {
                x.Id,
                x.Name,
                Assessment = x.RiskAssessments.OrderByDescending(r => r.EvaluatedAt).FirstOrDefault()
            })
            .Where(x => x.Assessment != null)
            .ToListAsync(cancellationToken);
        var items = rows.Select(x => Map(
            x.Assessment!.Id, x.Id, x.Name, x.Assessment.EvaluatedAt,
            x.Assessment.AlgorithmicScore, x.Assessment.MachineLearningScore,
            x.Assessment.HybridScore, x.Assessment.RiskLevel, x.Assessment.RiskReasons,
            x.Assessment.RecommendedAction, x.Assessment.ModelName, x.Assessment.ModelVersion,
            x.Assessment.ModelConfidence, x.Assessment.CorrelationId)).ToArray();
        return Ok(items);
    }

    private RiskHistoryItemResponse Map(
        string id,
        string machineId,
        string machineName,
        DateTimeOffset evaluatedAt,
        decimal algorithmicScore,
        decimal? mlScore,
        decimal hybridScore,
        RiskLevel riskLevel,
        string reasons,
        string recommendedAction,
        string? modelName,
        string? modelVersion,
        decimal? modelConfidence,
        string? correlationId) =>
        new(
            id,
            machineId,
            machineName,
            evaluatedAt,
            algorithmicScore,
            mlScore,
            new RiskPresentation(hybridScore, classificationService.GetLabel(riskLevel),
                classificationService.GetColorName(riskLevel), classificationService.GetColorHex(riskLevel)),
            reasons.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            recommendedAction,
            modelName,
            modelVersion,
            modelConfidence,
            correlationId);

    private static bool TryParseRiskLevel(string? value, out RiskLevel level)
    {
        level = RiskLevel.Low;
        if (string.IsNullOrWhiteSpace(value)) return false;
        level = value.Trim().ToUpperInvariant() switch
        {
            "DÜŞÜK" or "DUSUK" or "LOW" => RiskLevel.Low,
            "ORTA" or "MEDIUM" => RiskLevel.Medium,
            "YÜKSEK" or "YUKSEK" or "HIGH" => RiskLevel.High,
            "KRİTİK" or "KRITIK" or "CRITICAL" => RiskLevel.Critical,
            _ => level
        };
        return Enum.TryParse<RiskLevel>(value, true, out _) ||
               value.Trim().ToUpperInvariant() is "DÜŞÜK" or "DUSUK" or "ORTA" or
                   "YÜKSEK" or "YUKSEK" or "KRİTİK" or "KRITIK";
    }
}
