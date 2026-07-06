using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Payment.Application.Services;

/// <summary>
/// Saga compensation for step 1 (§8): release a previously-reserved amount back onto the department budget
/// after the payment provider declined the charge (INV-1012). Uses the same bounded ETag-CAS retry loop as
/// <see cref="ReserveBudgetService"/> so a concurrent reserve on the same department cannot undo the release.
/// Idempotent: a second call for the same <c>paymentId</c> is a no-op that returns the recorded result
/// (§10), so a redelivered failure event cannot pay a reservation back twice.
/// </summary>
public sealed class ReleaseReservationService
{
    private const int MaxCasAttempts = 8;
    private const string ReleasedReasonMarker = "release";

    private readonly IBudgetStore _budgets;
    private readonly IPaymentIdempotencyStore _idempotency;
    private readonly ILogger<ReleaseReservationService> _logger;

    public ReleaseReservationService(
        IBudgetStore budgets,
        IPaymentIdempotencyStore idempotency,
        ILogger<ReleaseReservationService> logger)
    {
        _budgets = budgets;
        _idempotency = idempotency;
        _logger = logger;
    }

    public async Task<ReleaseReservationResult> HandleAsync(ReleaseReservationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new ArgumentException("PaymentId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Department))
            throw new ArgumentException("Department is required.", nameof(request));
        if (request.AmountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(request), request.AmountUsd, "AmountUsd must be positive.");

        // Read the recorded reserve outcome for this paymentId to decide the branch:
        //   * null                                → no reservation ever ran; nothing to release.
        //   * Reserved=false, Reason="release"    → we already released once; replay the prior result.
        //   * Reserved=false, other/no reason     → reserve was refused (insufficient budget); nothing to release.
        //   * Reserved=true                       → live reservation; run the CAS release below.
        var reservation = await _idempotency.GetReserveResultAsync(request.PaymentId, ct);
        if (reservation is null)
        {
            _logger.LogInformation(
                "Release skipped for PaymentId {PaymentId}: no reservation record.", request.PaymentId);
            return new ReleaseReservationResult { Released = false, RemainingBudget = 0m };
        }

        var alreadyReleased = reservation.Reason?.StartsWith(ReleasedReasonMarker, StringComparison.Ordinal) == true;
        if (alreadyReleased)
        {
            _logger.LogInformation(
                "Release replayed for PaymentId {PaymentId} (idempotent, §10).", request.PaymentId);
            return new ReleaseReservationResult { Released = true, RemainingBudget = reservation.RemainingBudget };
        }

        if (!reservation.Reserved)
        {
            _logger.LogInformation(
                "Release skipped for PaymentId {PaymentId}: reserve was refused.", request.PaymentId);
            return new ReleaseReservationResult { Released = false, RemainingBudget = reservation.RemainingBudget };
        }

        for (var attempt = 1; attempt <= MaxCasAttempts; attempt++)
        {
            var snapshot = await _budgets.LoadAsync(request.Department, ct);
            if (snapshot is null)
                throw new InvalidOperationException($"Cannot release: no budget row for department '{request.Department}'.");

            var restored = DepartmentBudget.Rehydrate(snapshot.Budget.Department, snapshot.Budget.RemainingUsd + request.AmountUsd);

            var committed = await _budgets.TryWriteAsync(restored, snapshot.ETag, ct);
            if (!committed)
            {
                _logger.LogInformation(
                    "ETag conflict releasing budget for Department {Department} on attempt {Attempt}; retrying.",
                    request.Department, attempt);
                continue;
            }

            // Rewrite the reserve idempotency record so a second release finds "no active reservation".
            await _idempotency.SaveReserveResultAsync(request.PaymentId, new ReserveBudgetResult
            {
                Reserved = false,
                RemainingBudget = restored.RemainingUsd,
                Outcome = null,
                Reason = ReleasedReasonMarker
            }, ct);

            _logger.LogInformation(
                "Released {Amount} USD to Department {Department}; remaining {Remaining} (PaymentId {PaymentId}).",
                request.AmountUsd, request.Department, restored.RemainingUsd, request.PaymentId);

            return new ReleaseReservationResult { Released = true, RemainingBudget = restored.RemainingUsd };
        }

        throw new InvalidOperationException(
            $"Budget release for department '{request.Department}' exhausted {MaxCasAttempts} CAS attempts.");
    }
}
