using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Common;

namespace OnleyiciBakim.Contracts.Failures;

public sealed class FailureQuery : PaginationQuery
{
    public string? MachineId { get; init; }
    public string? FailureType { get; init; }
    public string? Severity { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
}

public sealed class CreateFailureRequest
{
    [Required, StringLength(30)]
    public string Id { get; init; } = null!;

    [Required, StringLength(30)]
    public string MachineId { get; init; } = null!;

    public DateTimeOffset OccurredAt { get; init; }
    public string? ComponentCode { get; init; }
    public string? ComponentName { get; init; }

    [Required, StringLength(150)]
    public string FailureType { get; init; } = null!;

    public string Severity { get; init; } = "Orta";
    public bool IsRecurring { get; init; }
    public bool IsPlannedDowntime { get; init; }

    [Range(0, int.MaxValue)]
    public int? FirstResponseMinutes { get; init; }

    [Range(0, int.MaxValue)]
    public int? InterventionMinutes { get; init; }

    [Range(0, int.MaxValue)]
    public int? DowntimeMinutes { get; init; }

    public string? RootCause { get; init; }
    public string? Description { get; init; }
    public string? PersonnelId { get; init; }
    public string? WorkOrderNumber { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? EstimatedCost { get; init; }

    public string? DataSource { get; init; }
}

public sealed class CloseFailureRequest
{
    [Required]
    public string Resolution { get; init; } = null!;

    public string? RootCause { get; init; }
    public string? PersonnelId { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }

    [Range(0, int.MaxValue)]
    public int? InterventionMinutes { get; init; }

    [Range(0, int.MaxValue)]
    public int? DowntimeMinutes { get; init; }
}

public sealed record FailureResponse(
    string Id,
    DateTimeOffset OccurredAt,
    string MachineId,
    string MachineName,
    string FailureType,
    string Severity,
    decimal DowntimeHours,
    string? Technician,
    string Status,
    string? RootCause,
    string? WorkOrderNumber,
    decimal? EstimatedCost,
    DateTimeOffset? ClosedAt);
