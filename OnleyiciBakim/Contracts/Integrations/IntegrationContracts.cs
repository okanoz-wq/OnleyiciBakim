using System.ComponentModel.DataAnnotations;
using OnleyiciBakim.Contracts.Failures;
using OnleyiciBakim.Contracts.Machines;
using OnleyiciBakim.Contracts.Risk;

namespace OnleyiciBakim.Contracts.Integrations;

public sealed class ErpMachineSyncRequest
{
    [Required, StringLength(100)]
    public string CorrelationId { get; init; } = null!;

    [Required]
    public IReadOnlyList<CreateMachineRequest> Machines { get; init; } = [];
}

public sealed class ErpFailureSyncRequest
{
    [Required, StringLength(100)]
    public string CorrelationId { get; init; } = null!;

    [Required]
    public IReadOnlyList<CreateFailureRequest> Failures { get; init; } = [];
}

public sealed class MlPredictionCallbackRequest : EvaluateMachineRiskRequest
{
    [Required, StringLength(30)]
    public string MachineId { get; init; } = null!;
}

public sealed record IntegrationResult(
    string CorrelationId,
    int ReceivedCount,
    int CreatedCount,
    int UpdatedCount,
    int RejectedCount,
    IReadOnlyList<string> Errors);
