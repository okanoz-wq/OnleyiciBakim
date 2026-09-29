using System.Net;
using System.Net.Http.Json;
using OnleyiciBakimSistemi.Models;
using OnleyiciBakimSistemi.Models.ViewModels;

namespace OnleyiciBakimSistemi.Services;

public sealed class BackendMaintenanceDataService(
    HttpClient httpClient) : IMaintenanceDataService
{
    public IReadOnlyList<string> FaultTypes { get; } =
        ["Motor arızası", "Rulman aşınması", "Elektrik/kontrol arızası", "Basınç/hidrolik arızası"];
    public IReadOnlyList<string> Severities { get; } =
        ["Düşük", "Orta", "Yüksek", "Kritik"];

    public async Task<DashboardViewModel> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var dashboard = await httpClient.GetFromJsonAsync<DashboardApiItem>(
            "api/v1/dashboard?months=6&riskyMachineCount=5", cancellationToken)
            ?? throw new InvalidOperationException("Dashboard API boş yanıt döndürdü.");

        return new DashboardViewModel
        {
            TotalMachines = dashboard.TotalMachines,
            RiskyMachineCount = dashboard.HighOrCriticalRiskMachines,
            PendingPlanCount = dashboard.PendingMaintenancePlans,
            AverageDowntimeHours = (double)dashboard.AverageFailureDurationHours,
            TopRiskMachines = dashboard.RiskiestMachines.Select(x => new Machine
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Location = x.Location ?? string.Empty,
                RiskScore = x.Risk.Score,
                RiskLevel = x.Risk.Level,
                RiskColorKey = x.Risk.RiskColorKey,
                NextMaintenanceDate = x.NextMaintenanceAt?.LocalDateTime ?? DateTime.MinValue
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<Machine>> GetMachinesAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllPagesAsync<MachineApiItem>(
            "api/v1/bakim-yonetimi/machines", cancellationToken);
        return items.Select(MapMachine).ToArray();
    }

    public async Task<Machine?> GetMachineAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"api/v1/bakim-yonetimi/machines/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<MachineDetailApiItem>(
            cancellationToken: cancellationToken);
        if (item is null)
            return null;
        return new Machine
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            Type = item.Type ?? string.Empty,
            Location = item.ProductionLine ?? item.Department ?? item.Branch ?? string.Empty,
            Status = item.Status,
            RiskScore = item.Risk.Score,
            RiskLevel = item.Risk.Level,
            RiskColorKey = item.Risk.RiskColorKey,
            InstallDate = item.InstallationYear.HasValue
                ? new DateTime(item.InstallationYear.Value, 1, 1)
                : DateTime.MinValue,
            LastMaintenanceDate = item.LastMaintenanceAt?.LocalDateTime ?? DateTime.MinValue,
            NextMaintenanceDate = item.RecommendedNextMaintenanceAt?.LocalDateTime ?? DateTime.MinValue,
            TotalFaultCount = item.TotalFailureCount
        };
    }

    public async Task<IReadOnlyList<FaultRecord>> GetFaultsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllPagesAsync<FailureApiItem>(
            "api/v1/bakim-yonetimi/failures", cancellationToken);
        return items.Select(MapFailure).ToArray();
    }

    public async Task<IReadOnlyList<FaultRecord>> GetFaultsByMachineAsync(
        string machineId,
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllPagesAsync<FailureApiItem>(
            $"api/v1/bakim-yonetimi/failures?machineId={Uri.EscapeDataString(machineId)}",
            cancellationToken);
        return items.Select(MapFailure).ToArray();
    }

    public async Task<IReadOnlyList<MaintenancePlanItem>> GetMaintenancePlansAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await GetAllPagesAsync<MaintenanceApiItem>(
            "api/v1/bakim-yonetimi/maintenance/plans", cancellationToken);
        return items.Select(x => new MaintenancePlanItem
        {
            Id = x.Id,
            MachineId = x.MachineId,
            MachineName = x.MachineName,
            PlannedDate = x.PlannedAt.LocalDateTime,
            Type = x.MaintenanceType,
            PredictedRiskScore = x.RiskScore,
            Confidence = NormalizePercentage(x.ModelConfidence),
            Status = TranslateStatus(x.Status),
            RecommendedAction = x.Recommendation ?? string.Empty
        }).ToArray();
    }

    public async Task<IReadOnlyList<MonthlyFaultPoint>> GetMonthlyFaultTrendAsync(
        int months,
        int? year,
        CancellationToken cancellationToken = default)
    {
        months = months == 12 ? 12 : 6;
        var yearQuery = year.HasValue ? $"&year={year.Value}" : string.Empty;
        var dashboard = await httpClient.GetFromJsonAsync<DashboardApiItem>(
            $"api/v1/dashboard?months={months}{yearQuery}", cancellationToken);
        return dashboard?.MonthlyFailureAnalysis.Select(x => new MonthlyFaultPoint
        {
            Month = $"{x.Label} {x.Year}",
            Count = x.FailureCount
        }).ToArray() ?? [];
    }

    public async Task<IReadOnlyList<int>> GetFaultYearsAsync(
        CancellationToken cancellationToken = default) =>
        (await GetFaultsAsync(cancellationToken))
            .Select(x => x.Date.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToArray();

    public async Task<IReadOnlyList<FaultTypeDistributionPoint>> GetFaultTypeDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var dashboard = await httpClient.GetFromJsonAsync<DashboardApiItem>(
            "api/v1/dashboard?months=6", cancellationToken);
        return dashboard?.FailureTypeDistribution.Select(x => new FaultTypeDistributionPoint
        {
            Name = x.Category,
            Value = x.Count
        }).ToArray() ?? [];
    }

    private static Machine MapMachine(MachineApiItem item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name,
        Type = item.Type ?? string.Empty,
        Location = item.Location ?? string.Empty,
        Status = item.Status,
        RiskScore = item.Risk.Score,
        RiskLevel = item.Risk.Level,
        RiskColorKey = item.Risk.RiskColorKey,
        LastMaintenanceDate = item.LastMaintenanceAt?.LocalDateTime ?? DateTime.MinValue,
        NextMaintenanceDate = item.NextMaintenanceAt?.LocalDateTime ?? DateTime.MinValue,
        TotalFaultCount = item.TotalFailureCount
    };

    private static FaultRecord MapFailure(FailureApiItem item) => new()
    {
        Id = item.Id,
        MachineId = item.MachineId,
        MachineName = item.MachineName,
        Date = item.OccurredAt.LocalDateTime,
        FaultType = item.FailureType,
        Severity = item.Severity,
        DowntimeHours = item.DowntimeHours,
        Technician = item.Technician ?? "Atanmamış",
        Resolved = IsResolvedStatus(item.Status)
    };

    private static bool IsResolvedStatus(string status) => status.Trim() switch
    {
        "Closed" or "Resolved" or "Kapalı" or "Kapandı" or "Kapatıldı" or
        "Çözüldü" or "Tamamlandı" => true,
        _ => false
    };

    private static string TranslateStatus(string status) => status switch
    {
        "Completed" => "Tamamlandı",
        "Tamamlandı" => "Tamamlandı",
        "InProgress" => "Devam Ediyor",
        "Devam Ediyor" => "Devam Ediyor",
        "Cancelled" => "İptal",
        "İptal" => "İptal",
        "Planlandı" => "Planlandı",
        _ => status
    };

    private static decimal? NormalizePercentage(decimal? value)
    {
        if (!value.HasValue)
            return null;

        var percentage = value.Value is >= 0m and <= 1m
            ? value.Value * 100m
            : value.Value;
        return Math.Clamp(percentage, 0m, 100m);
    }

    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        var items = new List<T>();
        var separator = relativeUrl.Contains('?') ? '&' : '?';
        var page = 1;
        var totalPages = 1;
        do
        {
            var response = await httpClient.GetFromJsonAsync<PagedEnvelope<T>>(
                $"{relativeUrl}{separator}page={page}&pageSize=200", cancellationToken)
                ?? throw new InvalidOperationException($"'{relativeUrl}' API yanıtı boş döndü.");
            items.AddRange(response.Items);
            totalPages = Math.Max(1, response.TotalPages);
            page++;
        } while (page <= totalPages);

        return items;
    }

    private sealed class PagedEnvelope<T>
    {
        public List<T> Items { get; init; } = [];
        public int TotalPages { get; init; }
    }
    private sealed record RiskApiItem(
        decimal Score, string Level, string RiskColorKey, string ColorHex);
    private sealed record MachineApiItem(
        string Id, string Code, string Name, string? Type, string? Location, string Status,
        DateTimeOffset? LastMaintenanceAt, DateTimeOffset? NextMaintenanceAt,
        int TotalFailureCount, RiskApiItem Risk);
    private sealed record MachineDetailApiItem(
        string Id, string Code, string Name, string? Type, string? Model, string Criticality,
        string Status, int? InstallationYear, string? Company, string? Branch, string? Department,
        string? ProductionLine, DateTimeOffset? LastMaintenanceAt,
        DateTimeOffset? RecommendedNextMaintenanceAt, int TotalFailureCount, RiskApiItem Risk);
    private sealed record FailureApiItem(
        string Id, DateTimeOffset OccurredAt, string MachineId, string MachineName,
        string FailureType, string Severity, decimal DowntimeHours, string? Technician, string Status);
    private sealed record MaintenanceApiItem(
        string Id, string MachineId, string MachineName, DateTimeOffset PlannedAt,
        string MaintenanceType, string Priority, string Status, decimal? ModelConfidence,
        decimal RiskScore,
        string? Recommendation);
    private sealed record DashboardApiItem(
        int TotalMachines,
        int HighOrCriticalRiskMachines,
        int PendingMaintenancePlans,
        decimal AverageFailureDurationHours,
        IReadOnlyList<MonthlyFailurePointApiItem> MonthlyFailureAnalysis,
        IReadOnlyList<CategoryApiItem> FailureTypeDistribution,
        IReadOnlyList<RiskiestApiItem> RiskiestMachines);
    private sealed record RiskiestApiItem(
        string Id, string Code, string Name, string? Location,
        DateTimeOffset? NextMaintenanceAt, RiskApiItem Risk);
    private sealed record MonthlyFailurePointApiItem(
        int Year, int Month, string Label, int FailureCount);
    private sealed record CategoryApiItem(string Category, int Count);
}
