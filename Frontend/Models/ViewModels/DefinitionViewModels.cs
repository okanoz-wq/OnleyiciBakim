using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnleyiciBakimSistemi.Models.ViewModels;

public sealed record DefinitionRowViewModel(
    string Id, string Code, string Name, bool Active, string? ParentId,
    string? ParentName, string? Type, string? Description,
    Dictionary<string, string?> Details);

public sealed record DefinitionMetadataViewModel(
    string Module, string Title, string CodeLabel, string NameLabel,
    string? ParentModule, string? ParentLabel, bool SupportsItems);

public sealed class DefinitionIndexViewModel
{
    public DefinitionMetadataViewModel Metadata { get; init; } = null!;
    public IReadOnlyList<DefinitionRowViewModel> Rows { get; init; } = [];
    public bool IncludeInactive { get; init; }
}

public sealed class DefinitionFormViewModel
{
    public string Module { get; set; } = string.Empty;
    public string? Id { get; set; }
    [ValidateNever]
    public DefinitionMetadataViewModel Metadata { get; set; } = null!;
    [ValidateNever]
    public IReadOnlyList<DefinitionRowViewModel> Parents { get; set; } = [];

    [Required(ErrorMessage = "Kod zorunludur.")]
    [StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(200)] public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Severity { get; set; }
    public string? Unit { get; set; }
    public string? Version { get; set; }
    public string? MachineType { get; set; }
    public string? Role { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "Tahmini ömür negatif olamaz.")]
    public int? EstimatedLife { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    [Range(0, 100)] public decimal? Minimum { get; set; }
    [Range(0, 100)] public decimal? Maximum { get; set; }
    [Range(0, 3650)] public int? RecommendedDays { get; set; }
    [Range(0, 100)] public int? AiWeight { get; set; }
    [Range(0, 100)] public int? RuleWeight { get; set; }
    public bool Active { get; set; } = true;
    public bool Flag { get; set; }
    public bool MaxIncluded { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    public List<ChecklistItemFormViewModel> Items { get; set; } = [];
}

public sealed class ChecklistItemFormViewModel
{
    [Required(ErrorMessage = "Kontrol maddesi zorunludur.")]
    public string Text { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool DescriptionRequired { get; set; }
}
