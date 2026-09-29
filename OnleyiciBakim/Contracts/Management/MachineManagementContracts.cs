using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;

namespace OnleyiciBakim.Contracts.Management;

public sealed class ManagedMachineQuery : PaginationQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Criticality { get; init; }
    public string? SourceSystem { get; init; }
    public string SortBy { get; init; } = "risk";
    public bool Descending { get; init; } = true;
}

public sealed class MachineUpsertRequest : IValidatableObject
{
    [Required(ErrorMessage = "Makine kodu zorunludur."), StringLength(50)]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "Makine adı zorunludur."), StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Firma seçimi zorunludur."), StringLength(20)]
    public string CompanyId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Şube seçimi zorunludur."), StringLength(20)]
    public string BranchId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Departman seçimi zorunludur."), StringLength(20)]
    public string DepartmentId { get; init; } = string.Empty;

    [Required(ErrorMessage = "Üretim hattı seçimi zorunludur."), StringLength(20)]
    public string ProductionLineId { get; init; } = string.Empty;

    [StringLength(50)]
    public string? Model { get; init; }

    [Range(0, 200, ErrorMessage = "Makine yaşı 0-200 arasında olmalıdır.")]
    public int Age { get; init; }

    [Required(ErrorMessage = "Kritiklik seviyesi zorunludur.")]
    public string Criticality { get; init; } = "Orta";

    [Range(1900, 2200, ErrorMessage = "Kurulum yılı geçerli değildir.")]
    public int InstallationYear { get; init; }

    [Required(ErrorMessage = "Durum zorunludur.")]
    public string Status { get; init; } = "Aktif";

    [DataType(DataType.Date)]
    public DateTime? LastMaintenanceDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InstallationYear > DateTime.UtcNow.Year)
            yield return new ValidationResult(
                "Kurulum tarihi gelecek bir tarih olamaz.",
                [nameof(InstallationYear)]);
        if (!MachineManagementRules.Criticalities.Contains(Criticality))
            yield return new ValidationResult(
                "Kritiklik seviyesi izin verilen değerlerden biri olmalıdır.",
                [nameof(Criticality)]);
        if (!MachineManagementRules.Statuses.Contains(Status))
            yield return new ValidationResult(
                "Makine durumu izin verilen değerlerden biri olmalıdır.",
                [nameof(Status)]);
        if (LastMaintenanceDate.HasValue && LastMaintenanceDate.Value.Date > DateTime.UtcNow.Date)
            yield return new ValidationResult(
                "Son bakım tarihi gelecek bir tarih olamaz.",
                [nameof(LastMaintenanceDate)]);
    }
}

public static class MachineManagementRules
{
    public static readonly string[] Criticalities = ["Düşük", "Orta", "Yüksek", "Kritik"];
    public static readonly string[] Statuses = ["Aktif", "İzlemede", "Riskli", "Bakımda", "Pasif"];
}

public sealed record ManagedMachineListItem(
    string Id,
    string Code,
    string Name,
    string? Model,
    string Location,
    string Status,
    string Criticality,
    int InstallationYear,
    DateTimeOffset? LastMaintenanceAt,
    decimal RiskScore,
    string RiskLevel,
    string RiskColorKey,
    string SourceSystem,
    bool CanManage,
    int RelatedRecordCount);

public sealed record ManagedMachineDetails(
    string Id,
    string Code,
    string Name,
    string? Model,
    int Age,
    int InstallationYear,
    string Criticality,
    string Status,
    DateTimeOffset? LastMaintenanceAt,
    string CompanyId,
    string Company,
    string BranchId,
    string Branch,
    string DepartmentId,
    string Department,
    string ProductionLineId,
    string ProductionLine,
    decimal RiskScore,
    string RiskLevel,
    string SourceSystem,
    string? ErpReferenceId,
    bool CanManage,
    bool HasRelatedRecords,
    int ComponentCount,
    int FailureCount,
    int MaintenanceCount,
    int TelemetryCount,
    int RiskAnalysisCount,
    IReadOnlyList<ManagedMachineComponentSummary> Components,
    IReadOnlyList<ManagedMachineHistorySummary> RecentFailures,
    IReadOnlyList<ManagedMachineHistorySummary> RecentMaintenances,
    IReadOnlyList<ManagedMachineTelemetrySummary> RecentTelemetry);

public sealed record ManagedMachineComponentSummary(
    int Id, string Code, string Name, string? Type, string? Criticality, bool IsActive);

public sealed record ManagedMachineHistorySummary(
    string Id, DateTimeOffset Date, string Type, string Status, string? Description);

public sealed record ManagedMachineTelemetrySummary(
    DateTimeOffset Date, decimal AnomalyScore, decimal ProductionIntensity,
    decimal VibrationAverage);

public sealed record ManagementLookupItem(string Id, string Code, string Name, string? ParentId = null);

public sealed record MachineManagementLookups(
    IReadOnlyList<ManagementLookupItem> Companies,
    IReadOnlyList<ManagementLookupItem> Branches,
    IReadOnlyList<ManagementLookupItem> Departments,
    IReadOnlyList<ManagementLookupItem> ProductionLines,
    IReadOnlyList<string> Criticalities,
    IReadOnlyList<string> Statuses,
    bool UseErpAsMasterSource);

public sealed record ManagementOperationResult(
    bool Success,
    string Message,
    string? Id = null,
    bool Deactivated = false);
