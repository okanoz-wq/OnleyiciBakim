using System.ComponentModel.DataAnnotations;

namespace OnleyiciBakim.Contracts.Workflows;

public sealed class AddFailureInterventionRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; init; }
    public string? PersonnelId { get; init; }
    [Required, StringLength(2000)]
    public string Note { get; init; } = null!;
}

public sealed class AssignAlertRequest
{
    [Required, StringLength(30)]
    public string PersonnelId { get; init; } = null!;
}

public sealed class NoActionRequest
{
    [Required, StringLength(2000)]
    public string Reason { get; init; } = null!;
}

public sealed class StartMaintenanceExecutionRequest
{
    [Required, StringLength(30)]
    public string ExecutionId { get; init; } = null!;
    public string? PersonnelId { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
}

public sealed class CompleteMaintenanceExecutionRequest
{
    [Required, StringLength(2000)]
    public string Result { get; init; } = null!;
    [Range(1, int.MaxValue)]
    public int DurationMinutes { get; init; }
    [Range(0, double.MaxValue)]
    public decimal? Cost { get; init; }
    public IReadOnlyList<ChecklistResultRequest> ChecklistResults { get; init; } = [];
    public IReadOnlyList<ReplacedPartRequest> ReplacedParts { get; init; } = [];
}

public sealed record ChecklistResultRequest(
    [property: Required] string ChecklistItemId,
    bool IsPassed,
    string? Note);

public sealed record ReplacedPartRequest(
    [property: Required] string PartCode,
    [property: Required] string PartName,
    [property: Range(0.001, double.MaxValue)] decimal Quantity);
