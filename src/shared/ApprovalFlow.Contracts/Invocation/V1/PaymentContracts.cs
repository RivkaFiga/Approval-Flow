using ApprovalFlow.Contracts.Enums;

namespace ApprovalFlow.Contracts.Invocation.V1;

/// <summary>
/// Workflow → Payment, saga step 1: reserve department budget via Dapr-state ETag CAS (§8). No overspend;
/// the budget never drops below 0.
/// </summary>
public sealed record ReserveBudgetRequest
{
    public string CorrelationId { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public decimal AmountUsd { get; init; }

    /// <summary>Idempotency key for the payment (equal to the paymentId, §10).</summary>
    public string PaymentId { get; init; } = string.Empty;
}

/// <summary>Payment → Workflow: result of the reserve step.</summary>
public sealed record ReserveBudgetResult
{
    public bool Reserved { get; init; }
    public decimal RemainingBudget { get; init; }

    /// <summary>
    /// Reason a reservation was refused. When non-null, the only meaningful value is
    /// <see cref="PaymentOutcome.InsufficientBudget"/>; <c>Paid</c> and <c>PaymentFailed</c>
    /// are not valid in a reserve result. Null means the reservation succeeded.
    /// </summary>
    public PaymentOutcome? Outcome { get; init; }

    public string? Reason { get; init; }
}

/// <summary>
/// Workflow → Payment, saga step 2: execute payment against the simulated provider with the idempotency
/// key so retries produce exactly one payment (§8, M10).
/// </summary>
public sealed record ExecutePaymentRequest
{
    public string CorrelationId { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public decimal AmountUsd { get; init; }
    public string PaymentId { get; init; } = string.Empty;
}

/// <summary>Payment → Workflow: result of the execute step.</summary>
public sealed record ExecutePaymentResult
{
    public PaymentOutcome Outcome { get; init; }
    public string PaymentId { get; init; } = string.Empty;

    /// <summary>Immutable ledger entry id written on success (audit, §8).</summary>
    public string? LedgerEntryId { get; init; }

    public string? Reason { get; init; }
}

/// <summary>
/// Workflow → Payment, compensation for step 1: release a reservation on payment failure (§8). Leaves no
/// orphaned reservation and no partial/double payment.
/// </summary>
public sealed record ReleaseReservationRequest
{
    public string CorrelationId { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public decimal AmountUsd { get; init; }
    public string PaymentId { get; init; } = string.Empty;
}

/// <summary>Payment → Workflow: result of the release compensation.</summary>
public sealed record ReleaseReservationResult
{
    public bool Released { get; init; }
    public decimal RemainingBudget { get; init; }
}
