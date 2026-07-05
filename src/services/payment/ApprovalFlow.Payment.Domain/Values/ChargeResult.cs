namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// Provider response for a single <see cref="ChargeCommand"/>. On <see cref="PaymentProviderOutcome.Succeeded"/>
/// the <see cref="ProviderReference"/> is populated with the opaque reference the ledger records (audit,
/// §8); on failure the <see cref="Reason"/> is populated with a human-readable explanation surfaced by
/// <c>item.finalized</c> in a later slice.
/// </summary>
public sealed record ChargeResult(
    PaymentProviderOutcome Outcome,
    string? ProviderReference,
    string? Reason)
{
    public static ChargeResult Success(string providerReference) =>
        new(PaymentProviderOutcome.Succeeded, providerReference, null);

    public static ChargeResult Failure(string reason) =>
        new(PaymentProviderOutcome.Failed, null, reason);
}
