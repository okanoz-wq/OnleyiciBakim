namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed class PaginationViewModel
{
    public const int DefaultPageSize = 20;

    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = "Index";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
    public int TotalCount { get; init; }
    public Dictionary<string, string> RouteValues { get; init; } = new();

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public int FirstItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalCount);
    public int StartPage => Math.Max(1, Math.Min(Page - 2, TotalPages - 4));
    public int EndPage => Math.Min(TotalPages, StartPage + 4);

    public Dictionary<string, string> RouteValuesFor(int page)
    {
        var values = new Dictionary<string, string>(RouteValues)
        {
            ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        return values;
    }
}
