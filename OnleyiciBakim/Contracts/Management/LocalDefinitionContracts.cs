using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakim.Contracts.Management;

public sealed record LocalDefinitionRow(
    string Id, string Code, string Name, bool Active, string? ParentId,
    string? ParentName, string? Type, string? Description, IReadOnlyDictionary<string, string?> Details);

public sealed class LocalDefinitionUpsertRequest
{
    [Required(ErrorMessage = "Kod zorunludur.")]
    [StringLength(50, ErrorMessage = "Kod en fazla 50 karakter olabilir.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(200, ErrorMessage = "Ad en fazla 200 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    public string? ParentId { get; set; }
    public string? MachineId { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public string? Unit { get; set; }
    public string? Version { get; set; }
    public string? MachineType { get; set; }
    public string? Role { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public int? RecommendedDays { get; set; }
    public int? AiWeight { get; set; }
    public int? RuleWeight { get; set; }
    public int? EstimatedLife { get; set; }
    public bool Active { get; set; } = true;
    public bool Flag { get; set; }
    public bool MaxIncluded { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public List<ChecklistItemRequest> Items { get; set; } = [];
}

public sealed class ChecklistItemRequest
{
    [Required(ErrorMessage = "Kontrol maddesi zorunludur.")]
    [StringLength(300)] public string Text { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool DescriptionRequired { get; set; }
}

public sealed record LocalDefinitionMetadata(
    string Module, string Title, string CodeLabel, string NameLabel,
    string? ParentModule, string? ParentLabel, bool SupportsItems = false);
