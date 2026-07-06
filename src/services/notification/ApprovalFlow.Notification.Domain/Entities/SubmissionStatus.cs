using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Notification.Domain.Rules;

namespace ApprovalFlow.Notification.Domain.Entities;

/// <summary>
/// Live status projection for one submitted item (§4, §11). Built from the four lifecycle events in §5.2 —
/// <c>invoice.submitted</c>, <c>decision.made</c>, <c>review.status</c>, <c>item.finalized</c> — so a
/// <c>GET /status</c> returns the current state (F2), not just the terminal outcome.
///
/// Idempotency (§10) is achieved by comparing each event's <c>OccurredAt</c> to <see cref="UpdatedAt"/>:
/// a replay or out-of-order event whose timestamp is not strictly newer is a no-op. The workflow publishes
/// its stage events in monotonic order, so this preserves the correct final projection under at-least-once
/// delivery without a separate processed-message table.
/// </summary>
public sealed class SubmissionStatus
{
    public Guid Id { get; private set; }
    public string TrackingId { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>Persisted as <c>int</c> so the <see cref="LifecycleStatus"/> enum can evolve without a migration.</summary>
    public int Status { get; private set; }

    /// <summary>Router route once decided; null while still <see cref="LifecycleStatus.Received"/>.</summary>
    public int? Route { get; private set; }

    public decimal? AmountUsd { get; private set; }

    /// <summary>Plain-language reason surfaced to the submitter (F2). Set on finalization or send-back.</summary>
    public string? Reason { get; private set; }

    public int? PaymentOutcome { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private SubmissionStatus() { }

    /// <summary>Creates the initial <see cref="LifecycleStatus.Received"/> projection when <c>invoice.submitted</c> arrives.</summary>
    public static SubmissionStatus CreateReceived(string trackingId, string correlationId, DateTimeOffset occurredAt)
    {
        return new SubmissionStatus
        {
            Id = Guid.NewGuid(),
            TrackingId = trackingId,
            CorrelationId = correlationId,
            Status = (int)LifecycleStatus.Received,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt
        };
    }

    /// <summary>
    /// <c>decision.made</c>: records the router's <see cref="Contracts.Enums.Route"/> and the USD amount, and
    /// advances the status to <see cref="LifecycleStatus.UnderReview"/> (mid-flight; the router's terminal
    /// routes still resolve to <see cref="LifecycleStatus.UnderReview"/> here — the terminal
    /// <see cref="LifecycleStatus"/> only lands on <c>item.finalized</c>, §5.2).
    /// </summary>
    public void ApplyDecision(Route route, decimal amountUsd, DateTimeOffset occurredAt)
    {
        if (!IsNewer(occurredAt)) return;
        Route = (int)route;
        AmountUsd = amountUsd;
        Status = (int)LifecycleStatus.UnderReview;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// <c>review.status</c>: reflects the HITL sub-state transition (awaiting-approval / awaiting-info / paying).
    /// For a send-back, <paramref name="whatWeStillNeed"/> becomes the submitter-facing reason (F5, §9).
    /// </summary>
    public void ApplyReviewSubState(ReviewSubState subState, string? whatWeStillNeed, DateTimeOffset occurredAt)
    {
        if (!IsNewer(occurredAt)) return;
        Status = (int)ReviewSubStateMap.ToLifecycle(subState);
        // The submitter-facing Reason for a send-back must not survive the state that produced it: once
        // the workflow moves on to AwaitingApproval (after InfoProvided) or Paying, F2 would otherwise
        // still show the stale "what we still need" until item.finalized eventually overwrites it.
        Reason = subState == ReviewSubState.AwaitingInfo && !string.IsNullOrWhiteSpace(whatWeStillNeed)
            ? whatWeStillNeed
            : null;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// <c>item.finalized</c>: terminal projection. The status becomes the final
    /// <see cref="LifecycleStatus"/> and the plain-language reason is recorded.
    /// </summary>
    public void ApplyFinalized(
        LifecycleStatus finalStatus,
        string reason,
        PaymentOutcome? paymentOutcome,
        DateTimeOffset occurredAt)
    {
        if (!IsNewer(occurredAt)) return;
        Status = (int)finalStatus;
        Reason = reason;
        PaymentOutcome = paymentOutcome is null ? null : (int)paymentOutcome.Value;
        UpdatedAt = occurredAt;
    }

    public LifecycleStatus CurrentStatus => (LifecycleStatus)Status;
    public Route? CurrentRoute => Route is null ? null : (Route)Route.Value;
    public PaymentOutcome? CurrentPaymentOutcome
        => PaymentOutcome is null ? null : (PaymentOutcome)PaymentOutcome.Value;

    private bool IsNewer(DateTimeOffset occurredAt) => occurredAt > UpdatedAt;
}
