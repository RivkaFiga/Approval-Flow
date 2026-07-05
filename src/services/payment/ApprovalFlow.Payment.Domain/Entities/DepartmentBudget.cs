using ApprovalFlow.Payment.Domain.Values;

namespace ApprovalFlow.Payment.Domain.Entities;

/// <summary>
/// One department's remaining budget (§7 in <c>policy.md</c>, §8 in ARCHITECTURE). Pure aggregate — no I/O
/// and no framework types — so the "budget never below 0" invariant (G4/M9) is provable in isolation.
///
/// The distributed concurrency guard (ETag CAS on the Dapr state key) lives in the infrastructure adapter;
/// this type only guarantees a *single* reservation attempt cannot take the balance negative. Combined,
/// they realize INV-1014A/B: two concurrent reserves cannot both succeed against the same key.
/// </summary>
public sealed class DepartmentBudget
{
    public string Department { get; private set; } = string.Empty;
    public decimal RemainingUsd { get; private set; }

    private DepartmentBudget() { }

    /// <summary>Bootstraps a fresh budget from a <see cref="DepartmentBudgetSeed"/> (see <c>BudgetSeeder</c>).</summary>
    public static DepartmentBudget Initialize(DepartmentBudgetSeed seed)
    {
        if (string.IsNullOrWhiteSpace(seed.Department))
            throw new ArgumentException("Department must be provided.", nameof(seed));
        if (seed.InitialUsd < 0m)
            throw new ArgumentOutOfRangeException(nameof(seed), seed.InitialUsd, "Initial budget cannot be negative.");

        return new DepartmentBudget
        {
            Department = seed.Department,
            RemainingUsd = seed.InitialUsd
        };
    }

    /// <summary>
    /// Rehydrates a persisted budget row (used by infrastructure adapters). Skips the "must not be negative"
    /// bootstrap check on <paramref name="remainingUsd"/> so a legitimately zeroed-out budget rehydrates,
    /// but still refuses negative values because that would violate the domain invariant.
    /// </summary>
    public static DepartmentBudget Rehydrate(string department, decimal remainingUsd)
    {
        if (string.IsNullOrWhiteSpace(department))
            throw new ArgumentException("Department must be provided.", nameof(department));
        if (remainingUsd < 0m)
            throw new ArgumentOutOfRangeException(nameof(remainingUsd), remainingUsd, "Remaining budget cannot be negative.");

        return new DepartmentBudget
        {
            Department = department,
            RemainingUsd = remainingUsd
        };
    }

    /// <summary>
    /// Attempts to reserve <paramref name="amountUsd"/> from the remaining balance. Returns
    /// <see cref="BudgetReservationOutcome.InsufficientBudget"/> without mutating state when the reservation
    /// would take the balance below 0. Non-positive amounts are rejected — reserving $0 has no business
    /// meaning and would let a caller silently succeed with no effect.
    /// </summary>
    public BudgetReservationOutcome TryReserve(decimal amountUsd)
    {
        if (amountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amountUsd), amountUsd, "Reserve amount must be positive.");

        if (amountUsd > RemainingUsd)
            return BudgetReservationOutcome.InsufficientBudget;

        RemainingUsd -= amountUsd;
        return BudgetReservationOutcome.Reserved;
    }
}
