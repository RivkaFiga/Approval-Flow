using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Payment.Infrastructure.Persistence;

/// <summary>
/// Idempotent bootstrap of department budgets on service start-up. For each configured seed the seeder
/// only writes if the store currently has no row for that department, so restarting the service does not
/// clobber a live balance mid-flight. Mirrors the <c>budgets</c> block in <c>sample-invoices.json</c>.
/// </summary>
public sealed class BudgetSeeder
{
    private readonly IBudgetStore _store;
    private readonly PaymentInfrastructureOptions _options;
    private readonly ILogger<BudgetSeeder> _logger;

    public BudgetSeeder(
        IBudgetStore store,
        IOptions<PaymentInfrastructureOptions> options,
        ILogger<BudgetSeeder> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var seed in _options.Seeds())
        {
            var existing = await _store.LoadAsync(seed.Department, ct);
            if (existing is not null)
            {
                _logger.LogDebug(
                    "Budget already present for Department {Department} (remaining {Remaining}); leaving untouched.",
                    seed.Department, existing.Budget.RemainingUsd);
                continue;
            }

            var budget = DepartmentBudget.Initialize(seed);
            await _store.WriteInitialAsync(budget, ct);
            _logger.LogInformation(
                "Seeded initial budget for Department {Department}: {InitialUsd} USD.",
                seed.Department, seed.InitialUsd);
        }
    }
}
