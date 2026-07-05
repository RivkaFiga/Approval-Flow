using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Ingests <c>item.finalized</c> and records the terminal status + plain-language reason so
/// <c>GET /status</c> returns the outcome the submitter needs (§5.2, F2). Handles out-of-order arrival by
/// creating the row if missing. Idempotent by monotonic <c>OccurredAt</c> (§10).
/// </summary>
public sealed class HandleItemFinalizedService
{
    private readonly ISubmissionStatusRepository _repo;
    private readonly ILogger<HandleItemFinalizedService> _logger;

    public HandleItemFinalizedService(
        ISubmissionStatusRepository repo,
        ILogger<HandleItemFinalizedService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task HandleAsync(ItemFinalizedV1 @event, CancellationToken ct = default)
    {
        var status = await _repo.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (status is null)
        {
            // Out-of-order arrival: seed at MinValue so the event's OccurredAt advances state.
            status = SubmissionStatus.CreateReceived(@event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue);
            await _repo.AddAsync(status, ct);
        }

        status.ApplyFinalized(@event.FinalStatus, @event.Reason, @event.PaymentOutcome, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected finalized {FinalStatus} for TrackingId {TrackingId}.",
            @event.FinalStatus, @event.TrackingId);
    }
}
