using ApprovalFlow.Approval.Application.Ports;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows.Activities;

/// <summary>
/// Removes the pending-approvals row (§9.1) when the approver's action resumes the workflow. Idempotent —
/// a missing row (replay or race with another handler) is treated as success.
/// </summary>
public sealed class ResolvePendingApprovalActivity : WorkflowActivity<string, object?>
{
    private readonly IPendingApprovalRepository _repo;

    public ResolvePendingApprovalActivity(IPendingApprovalRepository repo) => _repo = repo;

    public override async Task<object?> RunAsync(WorkflowActivityContext context, string trackingId)
    {
        await _repo.RemoveByTrackingIdAsync(trackingId);
        await _repo.SaveChangesAsync();
        return null;
    }
}
