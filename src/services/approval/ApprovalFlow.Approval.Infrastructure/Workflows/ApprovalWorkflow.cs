using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Approval.Infrastructure.Workflows.Activities;
using Dapr.Workflow;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Durable orchestration for one submitted item (§9, ADR-003). The instance id equals the item's
/// <c>trackingId</c> so HITL endpoints can raise <c>ApprovalDecision</c> events on it without an extra
/// lookup. Persists the workflow instance, then either:
/// <list type="bullet">
///   <item>terminal route (<c>auto_approve</c> | <c>reject</c> | <c>duplicate</c>) — publish
///     <c>item.finalized</c> directly;</item>
///   <item><c>human_review</c> — enqueue the pending-approvals projection, publish
///     <c>review.status(awaiting-approval)</c>, then <c>WaitForExternalEvent("ApprovalDecision")</c>.
///     <c>send-back</c> loops (publish <c>awaiting-info</c>, wait again); <c>approve</c> / <c>reject</c>
///     resolve the projection and publish <c>item.finalized</c>.</item>
/// </list>
/// The orchestrator body is deterministic: side-effects and clocks live in activities so a replay after a
/// service restart re-produces the same sequence (M11).
/// </summary>
public sealed class ApprovalWorkflow : Workflow<DecisionMadeV1, ItemFinalizedPublishRequest>
{
    public const string ApprovalDecisionEventName = "ApprovalDecision";

    public override async Task<ItemFinalizedPublishRequest> RunAsync(
        WorkflowContext context,
        DecisionMadeV1 input)
    {
        await context.CallActivityAsync(nameof(PersistInstanceActivity), input);

        if (input.Route != Route.HumanReview)
        {
            var terminal = BuildTerminalFinalized(input, ApprovalPath.Auto, approverComment: null);
            await context.CallActivityAsync(nameof(PublishItemFinalizedActivity), terminal);
            return terminal;
        }

        await context.CallActivityAsync(nameof(EnqueuePendingApprovalActivity), input);
        await context.CallActivityAsync(
            nameof(PublishReviewStatusActivity),
            new ReviewStatusPublishRequest
            {
                TrackingId = input.TrackingId,
                CorrelationId = input.CorrelationId,
                SubState = ReviewSubState.AwaitingApproval,
                WhatWeStillNeed = null
            });

        ApproverDecisionEvent decision;
        while (true)
        {
            decision = await context.WaitForExternalEventAsync<ApproverDecisionEvent>(ApprovalDecisionEventName);

            if (decision.Action != ApproverActionType.SendBack)
            {
                break;
            }

            await context.CallActivityAsync(
                nameof(PublishReviewStatusActivity),
                new ReviewStatusPublishRequest
                {
                    TrackingId = input.TrackingId,
                    CorrelationId = input.CorrelationId,
                    SubState = ReviewSubState.AwaitingInfo,
                    WhatWeStillNeed = decision.Comment
                });
        }

        await context.CallActivityAsync(nameof(ResolvePendingApprovalActivity), input.TrackingId);

        var finalized = BuildHumanFinalized(input, decision);
        await context.CallActivityAsync(nameof(PublishItemFinalizedActivity), finalized);
        return finalized;
    }

    private static ItemFinalizedPublishRequest BuildTerminalFinalized(
        DecisionMadeV1 input,
        ApprovalPath path,
        string? approverComment)
    {
        var (status, paymentOutcome) = input.Route switch
        {
            Route.AutoApprove => (LifecycleStatus.Paid, (PaymentOutcome?)PaymentOutcome.Paid),
            Route.Reject => (LifecycleStatus.Rejected, (PaymentOutcome?)null),
            Route.Duplicate => (LifecycleStatus.Duplicate, (PaymentOutcome?)null),
            _ => throw new InvalidOperationException(
                $"BuildTerminalFinalized called for non-terminal route {input.Route}.")
        };

        return new ItemFinalizedPublishRequest
        {
            TrackingId = input.TrackingId,
            CorrelationId = input.CorrelationId,
            FinalStatus = status,
            Reason = BuildReason(input, path, approverComment),
            PaymentOutcome = paymentOutcome,
            ApprovalPath = path,
            AmountUsd = input.AmountUsd
        };
    }

    private static ItemFinalizedPublishRequest BuildHumanFinalized(
        DecisionMadeV1 input,
        ApproverDecisionEvent decision)
    {
        var (status, paymentOutcome) = decision.Action switch
        {
            ApproverActionType.Approve => (LifecycleStatus.Paid, (PaymentOutcome?)PaymentOutcome.Paid),
            ApproverActionType.Reject => (LifecycleStatus.Rejected, (PaymentOutcome?)null),
            _ => throw new InvalidOperationException(
                $"BuildHumanFinalized reached with non-terminal action {decision.Action}.")
        };

        return new ItemFinalizedPublishRequest
        {
            TrackingId = input.TrackingId,
            CorrelationId = input.CorrelationId,
            FinalStatus = status,
            Reason = BuildReason(input, ApprovalPath.Human, decision.Comment),
            PaymentOutcome = paymentOutcome,
            ApprovalPath = ApprovalPath.Human,
            AmountUsd = input.AmountUsd
        };
    }

    private static string BuildReason(DecisionMadeV1 input, ApprovalPath path, string? approverComment)
    {
        var firstCited = input.CitedRules.FirstOrDefault();
        return input.Route switch
        {
            Route.AutoApprove => $"Auto-approved under autonomy policy (${input.AmountUsd:F2} USD, confidence {input.Confidence:F2}).",
            Route.Reject => firstCited is null
                ? "Rejected by policy."
                : $"Rejected by policy: {firstCited.RuleId} — {firstCited.Detail}",
            Route.Duplicate => "Duplicate submission — no additional processing performed.",
            Route.HumanReview => path == ApprovalPath.Human
                ? BuildHumanReason(approverComment)
                : "Escalated to human review.",
            _ => string.Empty
        };
    }

    private static string BuildHumanReason(string? approverComment)
        => string.IsNullOrWhiteSpace(approverComment)
            ? "Reviewed by approver."
            : $"Reviewed by approver: {approverComment}";
}
