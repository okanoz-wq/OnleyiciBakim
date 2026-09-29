using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.Id, x.EntityType, x.EntityId, x.Action, x.UserId,
                x.ChangesJson, x.CorrelationId, x.OccurredAtUtc
            }).ToListAsync(cancellationToken);
        return Ok(new { pageNumber = page, pageSize, totalCount = total, items });
    }
}
