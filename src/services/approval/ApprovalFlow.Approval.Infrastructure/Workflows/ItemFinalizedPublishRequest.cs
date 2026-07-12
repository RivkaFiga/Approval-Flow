using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Approval.Infrastructure.Workflows;

/// <summary>
/// Small deterministic input to <see cref="Activities.PublishItemFinalizedActivity"/>. Only primitives and
/// enums so replay stays stable; the activity is where a wall-clock timestamp is stamped.
/// </summary>
public sealed record ItemFinalizedPublishRequest
{
    public string TrackingId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public LifecycleStatus FinalStatus { get; init; }
    public string Reason { get; init; } = string.Empty;
    public PaymentOutcome? PaymentOutcome { get; init; }
    public ApprovalPath ApprovalPath { get; init; }
    public decimal AmountUsd { get; init; }

    /// <summary>Owning department budget key (§8). Populated for payment-eligible items; empty otherwise.</summary>
    public string Department { get; init; } = string.Empty;
}
