using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnleyiciBakim.Data;

namespace OnleyiciBakim.Controllers.Api;

[ApiController]
[Route("api/v1/reference-data")]
public sealed class ReferenceDataController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("organization")]
    public async Task<IActionResult> GetOrganization(CancellationToken cancellationToken)
    {
        var companies = await dbContext.Companies.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                Branches = x.Branches.Where(branch => branch.IsActive).Select(branch => new
                {
                    branch.Id,
                    branch.Code,
                    branch.Name,
                    Departments = branch.Departments.Where(department => department.IsActive).Select(department => new
                    {
                        department.Id,
                        department.Code,
                        department.Name,
                        ProductionLines = department.ProductionLines
                            .Where(line => line.IsActive)
                            .Select(line => new { line.Id, line.Code, line.Name, line.ShiftCount })
                    })
                })
            })
            .ToListAsync(cancellationToken);
        return Ok(companies);
    }

    [HttpGet("personnel")]
    public async Task<IActionResult> GetPersonnel(CancellationToken cancellationToken) =>
        Ok(await dbContext.Personnel.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.FullName, x.Role, x.Specialization })
            .ToListAsync(cancellationToken));
}
