namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// Outcome of a simulated payment-provider charge attempt (§1.4, §8). Pure domain value — the
/// transport-layer <c>Contracts.Enums.PaymentOutcome</c> is derived from this at the application layer.
/// </summary>
public enum PaymentProviderOutcome
{
    Succeeded,
    Failed
}
