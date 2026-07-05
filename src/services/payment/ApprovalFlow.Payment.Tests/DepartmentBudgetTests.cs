using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Xunit;

namespace ApprovalFlow.Payment.Tests;

public class DepartmentBudgetTests
{
    [Fact]
    public void Initialize_captures_seed_values()
    {
        var budget = DepartmentBudget.Initialize(new DepartmentBudgetSeed("marketing-2026Q2", 1000m));

        Assert.Equal("marketing-2026Q2", budget.Department);
        Assert.Equal(1000m, budget.RemainingUsd);
    }

    [Fact]
    public void Initialize_rejects_negative_initial_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DepartmentBudget.Initialize(new DepartmentBudgetSeed("marketing-2026Q2", -1m)));
    }

    [Fact]
    public void TryReserve_deducts_amount_when_sufficient()
    {
        var budget = DepartmentBudget.Initialize(new DepartmentBudgetSeed("engineering-2026Q2", 500m));

        var result = budget.TryReserve(200m);

        Assert.Equal(BudgetReservationOutcome.Reserved, result);
        Assert.Equal(300m, budget.RemainingUsd);
    }

    [Fact]
    public void TryReserve_allows_exact_balance()
    {
        var budget = DepartmentBudget.Initialize(new DepartmentBudgetSeed("engineering-2026Q2", 500m));

        var result = budget.TryReserve(500m);

        Assert.Equal(BudgetReservationOutcome.Reserved, result);
        Assert.Equal(0m, budget.RemainingUsd);
    }

    [Fact]
    public void TryReserve_refuses_when_insufficient_and_leaves_balance_untouched()
    {
        var budget = DepartmentBudget.Initialize(new DepartmentBudgetSeed("marketing-2026Q2", 400m));

        var result = budget.TryReserve(600m);

        Assert.Equal(BudgetReservationOutcome.InsufficientBudget, result);
        Assert.Equal(400m, budget.RemainingUsd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryReserve_rejects_non_positive_amount(int amount)
    {
        var budget = DepartmentBudget.Initialize(new DepartmentBudgetSeed("marketing-2026Q2", 100m));

        Assert.Throws<ArgumentOutOfRangeException>(() => budget.TryReserve(amount));
    }

    [Fact]
    public void Rehydrate_accepts_zero_remaining()
    {
        var budget = DepartmentBudget.Rehydrate("sales-2026Q2", 0m);

        Assert.Equal(0m, budget.RemainingUsd);
    }

    [Fact]
    public void Rehydrate_rejects_negative_remaining()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DepartmentBudget.Rehydrate("sales-2026Q2", -0.01m));
    }
}
