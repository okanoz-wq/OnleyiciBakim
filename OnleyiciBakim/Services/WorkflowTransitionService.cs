using OnleyiciBakim.Domain.Enums;
using OnleyiciBakim.Infrastructure;

namespace OnleyiciBakim.Services;

public interface IWorkflowTransitionService
{
    void EnsureFailureTransition(RecordStatus from, RecordStatus to);
    void EnsureMaintenancePlanTransition(MaintenancePlanStatus from, MaintenancePlanStatus to);
    void EnsureAlertTransition(AlertStatus from, AlertStatus to);
}

public sealed class WorkflowTransitionService : IWorkflowTransitionService
{
    private static readonly HashSet<(RecordStatus From, RecordStatus To)> FailureTransitions =
    [
        (RecordStatus.Open, RecordStatus.InProgress),
        (RecordStatus.InProgress, RecordStatus.Resolved),
        (RecordStatus.Resolved, RecordStatus.Closed),
        (RecordStatus.Open, RecordStatus.Cancelled),
        (RecordStatus.InProgress, RecordStatus.Cancelled)
    ];

    private static readonly HashSet<(MaintenancePlanStatus From, MaintenancePlanStatus To)> PlanTransitions =
    [
        (MaintenancePlanStatus.Suggested, MaintenancePlanStatus.Draft),
        (MaintenancePlanStatus.Suggested, MaintenancePlanStatus.Cancelled),
        (MaintenancePlanStatus.Draft, MaintenancePlanStatus.PendingApproval),
        (MaintenancePlanStatus.Draft, MaintenancePlanStatus.Cancelled),
        (MaintenancePlanStatus.PendingApproval, MaintenancePlanStatus.Approved),
        (MaintenancePlanStatus.PendingApproval, MaintenancePlanStatus.Cancelled),
        (MaintenancePlanStatus.Approved, MaintenancePlanStatus.Planned),
        (MaintenancePlanStatus.Planned, MaintenancePlanStatus.InProgress),
        (MaintenancePlanStatus.Planned, MaintenancePlanStatus.Cancelled),
        (MaintenancePlanStatus.InProgress, MaintenancePlanStatus.Completed)
    ];

    private static readonly HashSet<(AlertStatus From, AlertStatus To)> AlertTransitions =
    [
        (AlertStatus.Open, AlertStatus.Assigned),
        (AlertStatus.Open, AlertStatus.NoActionRequired),
        (AlertStatus.Assigned, AlertStatus.ConvertedToPlan),
        (AlertStatus.Assigned, AlertStatus.NoActionRequired),
        (AlertStatus.ConvertedToPlan, AlertStatus.Closed),
        (AlertStatus.NoActionRequired, AlertStatus.Closed),
        (AlertStatus.Assigned, AlertStatus.Closed)
    ];

    public void EnsureFailureTransition(RecordStatus from, RecordStatus to) =>
        Ensure(FailureTransitions.Contains((from, to)), "arıza", from, to);

    public void EnsureMaintenancePlanTransition(MaintenancePlanStatus from, MaintenancePlanStatus to) =>
        Ensure(PlanTransitions.Contains((from, to)), "bakım planı", from, to);

    public void EnsureAlertTransition(AlertStatus from, AlertStatus to) =>
        Ensure(AlertTransitions.Contains((from, to)), "erken uyarı", from, to);

    private static void Ensure<T>(bool allowed, string workflow, T from, T to)
    {
        if (!allowed)
            throw new ConflictException(
                $"{workflow} durum geçişi geçersiz: {from} -> {to}.");
    }
}
