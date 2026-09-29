using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakim.Contracts.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int PageNumber => Page;
    public bool HasPrevious => HasPreviousPage;
    public bool HasNext => HasNextPage;
}

public sealed record RiskPresentation(
    decimal Score,
    string Level,
    string Color,
    string ColorHex)
{
    public decimal RiskScore => Score;
    public string RiskLevel => Level;
    public string RiskLevelDisplayName => Level;
    public string RiskColorKey => Color;
}

public class PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 20;
}

public sealed record IdNameResponse(string Id, string Name);
