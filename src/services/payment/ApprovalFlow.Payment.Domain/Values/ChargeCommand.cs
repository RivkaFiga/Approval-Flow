namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// The provider-agnostic charge instruction sent to <c>IPaymentProvider</c> (§8 step 2). Carries the
/// idempotency key (<paramref name="PaymentId"/>) plus the fields a real provider adapter would need for
/// authorization/telemetry. Kept in the domain so the port contract stays free of framework types.
/// </summary>
public sealed record ChargeCommand(
    string PaymentId,
    string TrackingId,
    string CorrelationId,
    string Department,
    decimal AmountUsd);
