using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Dapr Workflow adapter for <see cref="IApprovalWorkflowScheduler"/>. The instance id is the item's
/// <c>trackingId</c> so the HITL endpoints can raise events on the same instance keyed by trackingId (§9).
/// Double-schedules for the same trackingId are swallowed — the workflow runtime is the arbiter of
/// uniqueness, so this defends the launcher against races the repository dedup check might miss.
/// </summary>
public sealed class DaprApprovalWorkflowScheduler : IApprovalWorkflowScheduler
{
    private readonly DaprWorkflowClient _client;
    private readonly ILogger<DaprApprovalWorkflowScheduler> _logger;

    public DaprApprovalWorkflowScheduler(
        DaprWorkflowClient client,
        ILogger<DaprApprovalWorkflowScheduler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task ScheduleAsync(DecisionMadeV1 input, CancellationToken ct = default)
    {
        try
        {
            await _client.ScheduleNewWorkflowAsync(
                name: nameof(ApprovalWorkflow),
                instanceId: input.TrackingId,
                input: input);
        }
        catch (Exception ex) when (IsAlreadyExists(ex))
        {
            _logger.LogInformation(
                "Workflow instance already scheduled for TrackingId {TrackingId}; ignoring redelivery.",
                input.TrackingId);
        }
    }

    private static bool IsAlreadyExists(Exception ex)
        => ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase);
}
