using ApprovalFlow.Notification.Application.Services;

namespace ApprovalFlow.Notification.Application.Ports;

/// <summary>Read-only aggregation port over the <c>SubmissionStatus</c> projection store (F8).</summary>
public interface IDashboardRepository
{
    Task<DashboardAggregate> GetAggregateAsync(CancellationToken ct = default);
}
