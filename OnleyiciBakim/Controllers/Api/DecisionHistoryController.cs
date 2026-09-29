using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Contracts.Common;
using OnleyiciBakim.Data;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/decision-history")]
public sealed class DecisionHistoryController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? machineId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.DecisionHistory.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(machineId))
            query = query.Where(x => x.MachineId == machineId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.DecidedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.MachineId, x.RiskAssessmentId, x.DecisionType,
                x.PreviousScore, x.FinalScore, x.Reason, x.UserId,
                x.DecidedAtUtc, x.ModelVersion, x.RuleSetVersion
            }).ToListAsync(cancellationToken);
        return Ok(new PagedResponse<object>(items.Cast<object>().ToArray(), page, pageSize, total));
    }
}
