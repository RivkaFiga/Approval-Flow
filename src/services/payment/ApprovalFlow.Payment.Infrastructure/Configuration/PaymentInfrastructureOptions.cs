using ApprovalFlow.Payment.Domain.Values;

namespace ApprovalFlow.Payment.Infrastructure.Configuration;

/// <summary>
/// Bound from the <c>Payment</c> configuration section (see <c>appsettings.json</c>). Holds the Dapr state
/// store component name and the initial department budgets used to bootstrap the store on first boot —
/// mirroring <c>sample-invoices.json → budgets</c> so the seeded state matches the fixtures.
/// </summary>
public sealed class PaymentInfrastructureOptions
{
    public const string SectionName = "Payment";

    /// <summary>Dapr state-store component name (durable backend per §5.3).</summary>
    public string StateStoreName { get; set; } = "approvalflow-state";

    /// <summary>Initial department budgets in USD, keyed by department id (e.g. <c>marketing-2026Q2</c>).</summary>
    public Dictionary<string, decimal> InitialBudgets { get; set; } = new();

    public IEnumerable<DepartmentBudgetSeed> Seeds()
        => InitialBudgets.Select(kvp => new DepartmentBudgetSeed(kvp.Key, kvp.Value));
}
