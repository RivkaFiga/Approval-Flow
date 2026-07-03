using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>
/// Gateway → Notification: the live submission status and plain-language reason for a tracking id
/// (§5.1, F2). Resolves a <c>received → under-review → awaiting-* → paying → final</c> state, not just the
/// terminal outcome.
/// </summary>
public sealed record SubmissionStatusResponse
{
    public string TrackingId { get; init; } = string.Empty;
    public LifecycleStatus Status { get; init; }

    /// <summary>Plain-language reason (present once the item has a decision/outcome).</summary>
    public string? Reason { get; init; }

    /// <summary>Deterministic route once decided; null while still <c>received</c>.</summary>
    public Route? Route { get; init; }

    public decimal? AmountUsd { get; init; }

    public PaymentOutcome? PaymentOutcome { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
