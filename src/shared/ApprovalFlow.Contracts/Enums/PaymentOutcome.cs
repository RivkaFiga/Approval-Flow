namespace ApprovalFlow.Contracts.Enums;

/// <summary>Outcome of the payment saga (§8).</summary>
public enum PaymentOutcome
{
    Paid,
    PaymentFailed,
    InsufficientBudget
}
