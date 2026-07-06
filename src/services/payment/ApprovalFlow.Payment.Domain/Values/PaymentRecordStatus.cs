namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// Terminal states of a <see cref="Entities.PaymentRecord"/> (§8). <c>Reserved</c> is transient — a saga run
/// leaves the record in one of the terminal states below so a redelivered <c>item.finalized</c> for the same
/// <c>trackingId</c> is a no-op (§10).
/// </summary>
public enum PaymentRecordStatus
{
    Reserved,
    Paid,
    Compensated,
    InsufficientBudget
}
