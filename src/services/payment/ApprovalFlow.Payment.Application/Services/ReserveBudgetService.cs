using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Payment.Application.Services;

/// <summary>
/// Saga step 1 (§8): atomically reserve department budget for one approved item. Combines the domain
/// invariant (never below 0) with a bounded ETag-CAS retry loop and idempotency memoization by
/// <c>paymentId</c>, so:
/// <list type="bullet">
///   <item><description>Concurrent reserves against the same key serialize on ETag mismatch — one wins,
///     the other retries and either wins or is refused with <see cref="PaymentOutcome.InsufficientBudget"/>
///     (INV-1014A/B).</description></item>
///   <item><description>A retry with the same <c>paymentId</c> is a no-op that returns the original
///     result (§10, M10).</description></item>
/// </list>
/// </summary>
public sealed class ReserveBudgetService
{
    private const int MaxCasAttempts = 8;

    private readonly IBudgetStore _budgets;
    private readonly IPaymentIdempotencyStore _idempotency;
    private readonly ILogger<ReserveBudgetService> _logger;

    public ReserveBudgetService(
        IBudgetStore budgets,
        IPaymentIdempotencyStore idempotency,
        ILogger<ReserveBudgetService> logger)
    {
        _budgets = budgets;
        _idempotency = idempotency;
        _logger = logger;
    }

    public async Task<ReserveBudgetResult> HandleAsync(ReserveBudgetRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new ArgumentException("PaymentId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Department))
            throw new ArgumentException("Department is required.", nameof(request));
        if (request.AmountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(request), request.AmountUsd, "AmountUsd must be positive.");

        var replay = await _idempotency.GetReserveResultAsync(request.PaymentId, ct);
        if (replay is not null)
        {
            _logger.LogInformation(
                "Replaying reserve result for PaymentId {PaymentId} (idempotent, §10).",
                request.PaymentId);
            return replay;
        }

        for (var attempt = 1; attempt <= MaxCasAttempts; attempt++)
        {
            var snapshot = await _budgets.LoadAsync(request.Department, ct);
            if (snapshot is null)
            {
                _logger.LogWarning(
                    "Reserve refused: no budget seeded for Department {Department}.",
                    request.Department);
                var missing = new ReserveBudgetResult
                {
                    Reserved = false,
                    RemainingBudget = 0m,
                    Outcome = PaymentOutcome.InsufficientBudget,
                    Reason = $"No budget configured for department '{request.Department}'."
                };
                await _idempotency.SaveReserveResultAsync(request.PaymentId, missing, ct);
                return missing;
            }

            var outcome = snapshot.Budget.TryReserve(request.AmountUsd);
            if (outcome == BudgetReservationOutcome.InsufficientBudget)
            {
                _logger.LogInformation(
                    "Reserve refused for Department {Department}: remaining {Remaining} < requested {Amount}.",
                    request.Department, snapshot.Budget.RemainingUsd, request.AmountUsd);
                var refused = new ReserveBudgetResult
                {
                    Reserved = false,
                    RemainingBudget = snapshot.Budget.RemainingUsd,
                    Outcome = PaymentOutcome.InsufficientBudget,
                    Reason = "Insufficient department budget."
                };
                await _idempotency.SaveReserveResultAsync(request.PaymentId, refused, ct);
                return refused;
            }

            var committed = await _budgets.TryWriteAsync(snapshot.Budget, snapshot.ETag, ct);
            if (!committed)
            {
                _logger.LogInformation(
                    "ETag conflict reserving budget for Department {Department} on attempt {Attempt}; retrying.",
                    request.Department, attempt);
                continue;
            }

            var success = new ReserveBudgetResult
            {
                Reserved = true,
                RemainingBudget = snapshot.Budget.RemainingUsd,
                Outcome = null,
                Reason = null
            };
            await _idempotency.SaveReserveResultAsync(request.PaymentId, success, ct);

            _logger.LogInformation(
                "Reserved {Amount} USD for Department {Department}; remaining {Remaining} (PaymentId {PaymentId}).",
                request.AmountUsd, request.Department, success.RemainingBudget, request.PaymentId);
            return success;
        }

        throw new InvalidOperationException(
            $"Budget reservation for department '{request.Department}' exhausted {MaxCasAttempts} CAS attempts.");
    }
}
