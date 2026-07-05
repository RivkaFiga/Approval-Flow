using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Payment.Application.Services;

/// <summary>
/// Saga step 2 (§8): charge the simulated provider against a previously reserved budget and append the
/// immutable ledger row. Preserves §10's "exactly one payment per idempotency key" invariant with two
/// layers, both keyed on <c>paymentId</c>:
/// <list type="number">
///   <item><description>An application-level replay from the idempotency store — a retry returns the
///     original <see cref="ExecutePaymentResult"/> (success or failure) without re-charging.</description></item>
///   <item><description>A database-level UNIQUE constraint on the ledger's <c>PaymentId</c> — if two
///     Execute calls slip past the idempotency check simultaneously, only one row commits; the other
///     reads the row back and replays.</description></item>
/// </list>
/// A prior <c>Reserve</c> with <c>Reserved=true</c> is a precondition; Execute refuses if none exists
/// (the workflow calls Reserve before Execute per §8, so this refusal only trips on bad callers or a bug).
/// </summary>
public sealed class ExecutePaymentService
{
    private readonly IPaymentIdempotencyStore _idempotency;
    private readonly IPaymentLedgerRepository _ledger;
    private readonly IPaymentProvider _provider;
    private readonly TimeProvider _clock;
    private readonly ILogger<ExecutePaymentService> _logger;

    public ExecutePaymentService(
        IPaymentIdempotencyStore idempotency,
        IPaymentLedgerRepository ledger,
        IPaymentProvider provider,
        TimeProvider clock,
        ILogger<ExecutePaymentService> logger)
    {
        _idempotency = idempotency;
        _ledger = ledger;
        _provider = provider;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ExecutePaymentResult> HandleAsync(ExecutePaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new ArgumentException("PaymentId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Department))
            throw new ArgumentException("Department is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.TrackingId))
            throw new ArgumentException("TrackingId is required.", nameof(request));
        if (request.AmountUsd <= 0m)
            throw new ArgumentOutOfRangeException(nameof(request), request.AmountUsd, "AmountUsd must be positive.");

        var replay = await _idempotency.GetExecuteResultAsync(request.PaymentId, ct);
        if (replay is not null)
        {
            _logger.LogInformation(
                "Replaying execute result for PaymentId {PaymentId} (idempotent, §10).",
                request.PaymentId);
            return replay;
        }

        var reservation = await _idempotency.GetReserveResultAsync(request.PaymentId, ct);
        if (reservation is null || !reservation.Reserved)
        {
            _logger.LogWarning(
                "Execute refused: no active reservation for PaymentId {PaymentId} (workflow must Reserve first).",
                request.PaymentId);
            var refused = new ExecutePaymentResult
            {
                Outcome = PaymentOutcome.PaymentFailed,
                PaymentId = request.PaymentId,
                LedgerEntryId = null,
                Reason = "No active reservation for this payment."
            };
            await _idempotency.SaveExecuteResultAsync(request.PaymentId, refused, ct);
            return refused;
        }

        var charge = await _provider.ChargeAsync(
            new ChargeCommand(
                request.PaymentId,
                request.TrackingId,
                request.CorrelationId,
                request.Department,
                request.AmountUsd),
            ct);

        if (charge.Outcome == PaymentProviderOutcome.Failed)
        {
            _logger.LogWarning(
                "Provider refused charge for PaymentId {PaymentId}: {Reason}.",
                request.PaymentId, charge.Reason);
            var failed = new ExecutePaymentResult
            {
                Outcome = PaymentOutcome.PaymentFailed,
                PaymentId = request.PaymentId,
                LedgerEntryId = null,
                Reason = charge.Reason ?? "Payment provider declined the charge."
            };
            await _idempotency.SaveExecuteResultAsync(request.PaymentId, failed, ct);
            return failed;
        }

        var entry = PaymentLedgerEntry.Create(
            request.PaymentId,
            request.TrackingId,
            request.CorrelationId,
            request.Department,
            request.AmountUsd,
            charge.ProviderReference!,
            _clock.GetUtcNow());

        var appended = await _ledger.TryAppendAsync(entry, ct);
        if (!appended)
        {
            // Concurrency safety net: two Execute calls raced past the idempotency check and one lost the
            // UNIQUE-constraint race. The winning row is authoritative — replay it.
            var existing = await _ledger.GetByPaymentIdAsync(request.PaymentId, ct)
                ?? throw new InvalidOperationException(
                    $"Ledger append refused for PaymentId {request.PaymentId} but no existing row was found.");

            _logger.LogInformation(
                "Ledger UNIQUE conflict on PaymentId {PaymentId}; replaying existing entry {LedgerEntryId}.",
                request.PaymentId, existing.Id);
            entry = existing;
        }

        var success = new ExecutePaymentResult
        {
            Outcome = PaymentOutcome.Paid,
            PaymentId = request.PaymentId,
            LedgerEntryId = entry.Id.ToString(),
            Reason = null
        };
        await _idempotency.SaveExecuteResultAsync(request.PaymentId, success, ct);

        _logger.LogInformation(
            "Paid {Amount} USD for Department {Department} (PaymentId {PaymentId}, LedgerEntryId {LedgerEntryId}).",
            request.AmountUsd, request.Department, request.PaymentId, success.LedgerEntryId);
        return success;
    }
}
