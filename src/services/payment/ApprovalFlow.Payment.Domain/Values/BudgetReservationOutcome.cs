namespace ApprovalFlow.Payment.Domain.Values;

/// <summary>
/// Outcome of applying <see cref="Entities.DepartmentBudget.TryReserve"/> — a pure domain value with no
/// dependency on the transport-layer <c>PaymentOutcome</c> enum. Kept separate so the invariant "budget
/// never below 0" (§8) can be unit-tested without a contracts reference.
/// </summary>
public enum BudgetReservationOutcome
{
    Reserved,
    InsufficientBudget
}
