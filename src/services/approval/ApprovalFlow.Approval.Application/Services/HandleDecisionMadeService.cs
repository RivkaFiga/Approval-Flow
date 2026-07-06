git show feature/payment-service:src/services/approval/ApprovalFlow.Approval.Application/Services/HandleDecisionMadeService.cs             
using System.Text.Json;
using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Domain.Entities;
using ApprovalFlow.Approval.Domain.Rules;
using ApprovalFlow.Approval.Domain.Values;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Approval.Application.Services;

/// <summary>
/// Orchestrates one <c>decision.made</c> pass (§9): persist workflow state, then publish either
/// <c>review.status(awaiting-approval)</c> (ApprovalRequired) or <c>item.finalized</c> (WorkflowCompleted)
/// according to the <see cref="WorkflowDecider"/>. Idempotent by <c>trackingId</c> — a redelivered
/// <c>decision.made</c> for an already-tracked item is a no-op (§10, redelivery de-dup for this stage).
/// </summary>
public sealed class HandleDecisionMadeService
{
    private readonly IWorkflowInstanceRepository _repo;
    private readonly IWorkflowEventPublisher _publisher;
    private readonly ILogger<HandleDecisionMadeService> _logger;

    public HandleDecisionMadeService(
        IWorkflowInstanceRepository repo,
        IWorkflowEventPublisher publisher,
        ILogger<HandleDecisionMadeService> logger)
    {
        _repo = repo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task HandleAsync(DecisionMadeV1 @event, CancellationToken ct = default)
    {
        if (await _repo.ExistsByTrackingIdAsync(@event.TrackingId, ct))
        {
            _logger.LogInformation(
                "Workflow already tracked for TrackingId {TrackingId}; skipping redelivery.",
                @event.TrackingId);
            return;
        }

        var state = WorkflowDecider.StateFor(@event.Route);
        var nextStep = WorkflowDecider.NextStepFor(@event.Route);

        var instance = WorkflowInstance.Create(
            @event.TrackingId,
            @event.CorrelationId,
            @event.Route,
            state,
            @event.Recommendation,
            @event.Confidence,
            @event.AmountUsd,
            @event.Department,
            JsonSerializer.Serialize(@event.CitedRules),
            @event.FraudSignal?.Detected ?? false,
            @event.FraudSignal?.Reason);

        await _repo.AddAsync(instance, ct);
        await _repo.SaveChangesAsync(ct);

        switch (nextStep)
        {
            case WorkflowNextStep.RequireHumanApproval:
                await PublishApprovalRequiredAsync(@event, ct);
                break;
            case WorkflowNextStep.Complete:
                await PublishWorkflowCompletedAsync(@event, ct);
                break;
            default:
                throw new InvalidOperationException($"Unhandled next step {nextStep}.");
        }

        _logger.LogInformation(
            "Workflow {NextStep} for TrackingId {TrackingId} (route {Route}, state {State}).",
            nextStep, @event.TrackingId, @event.Route, state);
    }

    private Task PublishApprovalRequiredAsync(DecisionMadeV1 @event, CancellationToken ct)
    {
        var review = new ReviewStatusV1
        {
            TrackingId = @event.TrackingId,
            CorrelationId = @event.CorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            SubState = ReviewSubState.AwaitingApproval,
            WhatWeStillNeed = null
        };
        return _publisher.PublishReviewStatusAsync(review, ct);
    }

    private Task PublishWorkflowCompletedAsync(DecisionMadeV1 @event, CancellationToken ct)
    {
        var (status, paymentOutcome, path) = TerminalOutcomeFor(@event.Route);
        var finalized = new ItemFinalizedV1
        {
            TrackingId = @event.TrackingId,
            CorrelationId = @event.CorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            FinalStatus = status,
            Reason = BuildReason(@event),
            PaymentOutcome = paymentOutcome,
            ApprovalPath = path,
            AmountUsd = @event.AmountUsd,
            Department = @event.Department
        };
        return _publisher.PublishItemFinalizedAsync(finalized, ct);
    }

    /// <remarks>
    /// The auto-approve branch currently emits <see cref="LifecycleStatus.Paid"/> with
    /// <see cref="PaymentOutcome.Paid"/> because the payment saga (§8) has not landed yet. Once the saga
    /// is wired, the auto-approve branch will hand off to Payment and only emit
    /// <c>item.finalized</c> when the saga terminates — this slice keeps the event contract intact.
    /// </remarks>
    private static (LifecycleStatus, PaymentOutcome?, ApprovalPath) TerminalOutcomeFor(Route route) => route switch
    {
        Route.AutoApprove => (LifecycleStatus.Paid, PaymentOutcome.Paid, ApprovalPath.Auto),
        Route.Reject => (LifecycleStatus.Rejected, null, ApprovalPath.Auto),
        Route.Duplicate => (LifecycleStatus.Duplicate, null, ApprovalPath.Auto),
        _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Route is not terminal.")
    };

    private static string BuildReason(DecisionMadeV1 @event)
    {
        var firstCited = @event.CitedRules.FirstOrDefault();
        return @event.Route switch
        {
            Route.AutoApprove => $"Auto-approved under autonomy policy (${@event.AmountUsd:F2} USD, confidence {@event.Confidence:F2})
.",
            Route.Reject => firstCited is null
                ? "Rejected by policy."
                : $"Rejected by policy: {firstCited.RuleId} — {firstCited.Detail}",
            Route.Duplicate => "Duplicate submission — no additional processing performed.",
            _ => string.Empty
        };
    }
}