using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakim.Contracts.Definitions;

public class CreateCodeDefinitionRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;

    [Required, StringLength(50)]
    public string Code { get; init; } = null!;

    [Required, StringLength(200)]
    public string Name { get; init; } = null!;

    public string? Description { get; init; }
}

public sealed class CreateFailureCodeRequest : CreateCodeDefinitionRequest
{
    public string DefaultSeverity { get; init; } = "Orta";
}

public sealed class CreateMaintenanceTypeRequest : CreateCodeDefinitionRequest
{
    [Range(1, 36500)]
    public int? DefaultPeriodDays { get; init; }
}

public sealed class CreateChecklistRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;

    [Required, StringLength(200)]
    public string Name { get; init; } = null!;

    public string? MachineType { get; init; }

    [Required]
    public string MaintenanceTypeId { get; init; } = null!;

    [MinLength(1)]
    public IReadOnlyList<ChecklistItemRequest> Items { get; init; } = [];
}

public sealed record ChecklistItemRequest(
    [property: Required] string Description,
    bool IsRequired = true);

public sealed class UpsertParameterRequest
{
    [Required, StringLength(50)]
    public string Id { get; init; } = null!;

    [Required, StringLength(100)]
    public string Group { get; init; } = null!;

    [Required, StringLength(100)]
    public string Key { get; init; } = null!;

    [Required]
    public string Value { get; init; } = null!;

    public string DataType { get; init; } = "decimal";
    public string? Description { get; init; }
}
