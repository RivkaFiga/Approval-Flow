using System.Text.Json;
using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows.Activities;

/// <summary>
/// Inserts a row into the queryable pending-approvals projection (§9.1) so <c>GET /queue</c> (F4) can list
/// the waiting item. Idempotent by <c>TrackingId</c>: a replay after a partial commit sees the row already
/// present and skips, so real database errors still propagate instead of being silently swallowed.
/// </summary>
public sealed class EnqueuePendingApprovalActivity : WorkflowActivity<DecisionMadeV1, object?>
{
    private readonly IPendingApprovalRepository _repo;

    public EnqueuePendingApprovalActivity(IPendingApprovalRepository repo) => _repo = repo;

    public override async Task<object?> RunAsync(WorkflowActivityContext context, DecisionMadeV1 input)
    {
        if (await _repo.ExistsByTrackingIdAsync(input.TrackingId))
        {
            return null;
        }

        var pending = PendingApproval.Create(
            input.TrackingId,
            input.CorrelationId,
            input.Recommendation,
            input.Confidence,
            JsonSerializer.Serialize(input.CitedRules),
            input.AmountUsd,
            input.Department);

        await _repo.AddAsync(pending);
        await _repo.SaveChangesAsync();
        return null;
    }
}
