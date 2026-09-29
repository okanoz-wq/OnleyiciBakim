namespace OnleyiciBakim.ViewModels;

public sealed record DevTestViewModel(
    int TotalMachines,
    int OpenFailures,
    int PendingMaintenance,
    IReadOnlyList<DevRiskCount> RiskCounts,
    IReadOnlyList<DevMachineItem> Machines,
    IReadOnlyList<DevBoundaryItem> Boundaries,
    IReadOnlyList<DevIntegrationLogItem> IntegrationLogs);

public sealed record DevRiskCount(string Level, int Count, string ColorKey);
public sealed record DevMachineItem(string Id, string Code, string Name, decimal RiskScore, string RiskLevel);
public sealed record DevBoundaryItem(decimal Score, string Level, string ColorKey);
public sealed record DevIntegrationLogItem(
    long Id, string SystemName, string EntityType, string Status, DateTimeOffset ProcessedAt);
