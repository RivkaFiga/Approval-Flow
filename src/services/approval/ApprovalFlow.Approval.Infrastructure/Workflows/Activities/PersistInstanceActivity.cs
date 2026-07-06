using System.Text.Json;
using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;
using ApprovalFlow.Approval.Domain.Rules;
using ApprovalFlow.Contracts.Events.V1;
using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Infrastructure.Workflows.Activities;

/// <summary>
/// Writes the initial <see cref="WorkflowInstance"/> row to Postgres (§11). Idempotent by <c>TrackingId</c>
/// so a workflow replay is a no-op.
/// </summary>
public sealed class PersistInstanceActivity : WorkflowActivity<DecisionMadeV1, object?>
{
    private readonly IWorkflowInstanceRepository _repo;
    private readonly ILogger<PersistInstanceActivity> _logger;

    public PersistInstanceActivity(
        IWorkflowInstanceRepository repo,
        ILogger<PersistInstanceActivity> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public override async Task<object?> RunAsync(WorkflowActivityContext context, DecisionMadeV1 input)
    {
        if (await _repo.ExistsByTrackingIdAsync(input.TrackingId))
        {
            _logger.LogInformation(
                "WorkflowInstance already persisted for TrackingId {TrackingId}; skipping.",
                input.TrackingId);
            return null;
        }

        var state = WorkflowDecider.StateFor(input.Route);
        var instance = WorkflowInstance.Create(
            input.TrackingId,
            input.CorrelationId,
            input.Route,
            state,
            input.Recommendation,
            input.Confidence,
            input.AmountUsd,
            input.Department,
            JsonSerializer.Serialize(input.CitedRules),
            input.FraudSignal?.Detected ?? false,
            input.FraudSignal?.Reason);

        await _repo.AddAsync(instance);
        await _repo.SaveChangesAsync();
        return null;
    }
}
