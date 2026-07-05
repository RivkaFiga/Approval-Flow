using ApprovalFlow.Payment.Domain.Entities;

namespace ApprovalFlow.Payment.Application.Ports;

/// <summary>
/// Persistence port for <see cref="DepartmentBudget"/> using an optimistic-concurrency (ETag CAS) contract
/// so §8 step 1 has no overspend under concurrent approvals (INV-1014A/B). Adapters implement this against
/// the Dapr state store (durable backend per §5.3).
/// </summary>
public interface IBudgetStore
{
    /// <summary>
    /// Loads a department's budget together with the store's opaque ETag. Returns <c>null</c> when the
    /// department has not been seeded — the caller decides whether that is a hard error (production)
    /// or a bootstrap trigger (dev).
    /// </summary>
    Task<BudgetSnapshot?> LoadAsync(string department, CancellationToken ct = default);

    /// <summary>
    /// Attempts a compare-and-set write of <paramref name="budget"/>, guarded by the ETag returned from
    /// <see cref="LoadAsync"/>. Returns <c>false</c> when the ETag no longer matches — the caller must
    /// re-read and retry (§8).
    /// </summary>
    Task<bool> TryWriteAsync(DepartmentBudget budget, string etag, CancellationToken ct = default);

    /// <summary>Unconditional write used by the initial seeder only (see <c>BudgetSeeder</c>).</summary>
    Task WriteInitialAsync(DepartmentBudget budget, CancellationToken ct = default);
}

/// <summary>Envelope pairing a loaded aggregate with its store-specific ETag (opaque to the domain).</summary>
public sealed record BudgetSnapshot(DepartmentBudget Budget, string ETag);
