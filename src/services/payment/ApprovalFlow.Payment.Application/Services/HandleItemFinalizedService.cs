using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Domain.Entities;
using ApprovalFlow.Payment.Domain.Values;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Payment.Application.Services;

/// <summary>
/// End-to-end flow entry point: <c>item.finalized</c> → Payment saga → <c>payment.completed</c> (§8).
/// Runs the three saga steps in order:
/// <list type="number">
///   <item><description>Reserve — ETag-CAS budget draw; on refusal the saga terminates with
///     <see cref="PaymentOutcome.InsufficientBudget"/> and no ledger row (§8).</description></item>
///   <item><description>Pay — idempotent charge + append-only ledger row (§10, M10).</description></item>
///   <item><description>Compensate — on provider failure only, releases the reservation so the budget is
///     restored (INV-1012) and no partial or double payment occurs.</description></item>
/// </list>
/// A <see cref="PaymentRecord"/> is written before Reserve so a redelivered <c>item.finalized</c> for the
/// same <c>trackingId</c> replays the prior outcome without touching the budget or the provider (§10). Only
/// approved items (those carrying a <see cref="ItemFinalizedV1.PaymentOutcome"/>) run the saga; rejected or
/// duplicate items are dropped silently — <c>Notification</c> already surfaces their terminal status from
/// the same <c>item.finalized</c> event (§5.2).
/// </summary>
public sealed class HandleItemFinalizedService
{
    private readonly IPaymentRecordRepository _records;
    private readonly ReserveBudgetService _reserve;
    private readonly ExecutePaymentService _execute;
    private readonly ReleaseReservationService _compensate;
    private readonly IPaymentEventPublisher _publisher;
    private readonly TimeProvider _clock;
    private readonly ILogger<HandleItemFinalizedService> _logger;

    public HandleItemFinalizedService(
        IPaymentRecordRepository records,
        ReserveBudgetService reserve,
        ExecutePaymentService execute,
        ReleaseReservationService compensate,
        IPaymentEventPublisher publisher,
        TimeProvider clock,
        ILogger<HandleItemFinalizedService> logger)
    {
        _records = records;
        _reserve = reserve;
        _execute = execute;
        _compensate = compensate;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(ItemFinalizedV1 @event, CancellationToken ct = default)
    {
        // Payment only runs for approved items. Rejected / duplicate finalizations carry no
        // PaymentOutcome (§5.2, ItemFinalizedV1) and never enter the saga.
        if (@event.PaymentOutcome is null)
        {
            _logger.LogDebug(
                "Skipping item.finalized for TrackingId {TrackingId}: no payment leg.", @event.TrackingId);
            return;
        }

        if (string.IsNullOrWhiteSpace(@event.Department))
            throw new InvalidOperationException(
                $"item.finalized for TrackingId {@event.TrackingId} is missing the Department needed by the saga.");
        if (@event.AmountUsd <= 0m)
            throw new InvalidOperationException(
                $"item.finalized for TrackingId {@event.TrackingId} has non-positive amount {@event.AmountUsd}.");

        // Redelivery de-dup (§10): if a record exists for this trackingId, replay its outcome rather than
        // re-running the saga. The publisher is called again so a lost payment.completed event can catch up
        // — Notification stays idempotent by its own OccurredAt watermark.
        var existing = await _records.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Replaying prior payment outcome {Status} for TrackingId {TrackingId} (redelivery).",
                existing.Status, @event.TrackingId);
            await PublishCompletedAsync(existing, ct);
            return;
        }

        // §10: paymentId is derived deterministically from trackingId, so retries of the same item collapse
        // onto the same idempotency key across reserve / execute / release.
        var paymentId = $"pay-{@event.TrackingId}";

        var record = PaymentRecord.StartReserved(
            paymentId,
            @event.TrackingId,
            @event.CorrelationId,
            @event.Department,
            @event.AmountUsd,
            _clock.GetUtcNow());

        await _records.AddAsync(record, ct);
        // Flushed before Reserve so a crash between Reserve and SaveChanges cannot leave a claimed budget
        // without its saga record — the record is what the redelivery replay above keys on.
        await _records.SaveChangesAsync(ct);

        var reserveResult = await _reserve.HandleAsync(new ReserveBudgetRequest
        {
            CorrelationId = @event.CorrelationId,
            TrackingId = @event.TrackingId,
            Department = @event.Department,
            AmountUsd = @event.AmountUsd,
            PaymentId = paymentId
        }, ct);

        if (!reserveResult.Reserved)
        {
            var reason = reserveResult.Reason ?? "Insufficient department budget.";
            record.MarkInsufficientBudget(reason, _clock.GetUtcNow());
            await _records.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Saga terminated for TrackingId {TrackingId}: {Reason}.", @event.TrackingId, reason);
            await PublishCompletedAsync(record, ct);
            return;
        }

        var executeResult = await _execute.HandleAsync(new ExecutePaymentRequest
        {
            CorrelationId = @event.CorrelationId,
            TrackingId = @event.TrackingId,
            Department = @event.Department,
            AmountUsd = @event.AmountUsd,
            PaymentId = paymentId
        }, ct);

        if (executeResult.Outcome == PaymentOutcome.Paid)
        {
            record.MarkPaid(executeResult.LedgerEntryId!, _clock.GetUtcNow());
            await _records.SaveChangesAsync(ct);
            await PublishCompletedAsync(record, ct);
            return;
        }

        // Provider declined: run the compensation (release reservation) so the budget is restored — no
        // orphaned reservation, no partial payment (INV-1012, §8).
        var releaseResult = await _compensate.HandleAsync(new ReleaseReservationRequest
        {
            CorrelationId = @event.CorrelationId,
            TrackingId = @event.TrackingId,
            Department = @event.Department,
            AmountUsd = @event.AmountUsd,
            PaymentId = paymentId
        }, ct);

        var failureReason = executeResult.Reason ?? "Payment provider declined the charge.";
        record.MarkCompensated(failureReason, _clock.GetUtcNow());
        await _records.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Compensated saga for TrackingId {TrackingId}: {Reason}. Budget restored to {Remaining} USD.",
            @event.TrackingId, failureReason, releaseResult.RemainingBudget);

        await PublishCompletedAsync(record, ct);
    }

    private Task PublishCompletedAsync(PaymentRecord record, CancellationToken ct)
    {
        var completed = new PaymentCompletedV1
        {
            CorrelationId = record.CorrelationId,
            OccurredAt = _clock.GetUtcNow(),
            TrackingId = record.TrackingId,
            PaymentId = record.PaymentId,
            Department = record.Department,
            AmountUsd = record.AmountUsd,
            Outcome = MapOutcome(record.Status),
            LedgerEntryId = record.LedgerEntryId,
            Reason = record.Reason
        };
        return _publisher.PublishPaymentCompletedAsync(completed, ct);
    }

    private static PaymentOutcome MapOutcome(PaymentRecordStatus status) => status switch
    {
        PaymentRecordStatus.Paid => PaymentOutcome.Paid,
        PaymentRecordStatus.Compensated => PaymentOutcome.PaymentFailed,
        PaymentRecordStatus.InsufficientBudget => PaymentOutcome.InsufficientBudget,
        // Reserved is transient; the saga does not publish while it is still in flight.
        _ => throw new InvalidOperationException($"Cannot publish payment.completed for transient status {status}.")
    };
}
