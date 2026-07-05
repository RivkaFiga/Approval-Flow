using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using Dapr.Client;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// Dapr-state ETag-CAS adapter for <see cref="IBudgetStore"/>. Backs §8 step 1: the state component's
/// optimistic concurrency guarantees that two concurrent reserves against the same key cannot both commit
/// (INV-1014A/B). Component durability is a config concern per §5.3.
/// </summary>
public sealed class DaprBudgetStore : IBudgetStore
{
    private const string KeyPrefix = "budgets|";

    private readonly DaprClient _dapr;
    private readonly PaymentInfrastructureOptions _options;

    public DaprBudgetStore(DaprClient dapr, IOptions<PaymentInfrastructureOptions> options)
    {
        _dapr = dapr;
        _options = options.Value;
    }

    public async Task<BudgetSnapshot?> LoadAsync(string department, CancellationToken ct = default)
    {
        var (state, etag) = await _dapr.GetStateAndETagAsync<BudgetState?>(
            _options.StateStoreName, Key(department), cancellationToken: ct);

        if (state is null || string.IsNullOrEmpty(etag))
            return null;

        var budget = state.ToDomain();
        return new BudgetSnapshot(budget, etag);
    }

    public async Task<bool> TryWriteAsync(DepartmentBudget budget, string etag, CancellationToken ct = default)
    {
        var state = BudgetState.From(budget);
        return await _dapr.TrySaveStateAsync(
            _options.StateStoreName, Key(budget.Department), state, etag, cancellationToken: ct);
    }

    public Task WriteInitialAsync(DepartmentBudget budget, CancellationToken ct = default)
    {
        var state = BudgetState.From(budget);
        return _dapr.SaveStateAsync(_options.StateStoreName, Key(budget.Department), state, cancellationToken: ct);
    }

    private static string Key(string department) => KeyPrefix + department;

    /// <summary>
    /// On-the-wire shape of the budget row. Kept separate from the domain aggregate so the JSON contract in
    /// the state store is stable even if the aggregate evolves.
    /// </summary>
    private sealed record BudgetState(string Department, decimal RemainingUsd)
    {
        public static BudgetState From(DepartmentBudget budget) => new(budget.Department, budget.RemainingUsd);

        public DepartmentBudget ToDomain() => DepartmentBudget.Rehydrate(Department, RemainingUsd);
    }
}
