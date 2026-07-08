using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
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
        // GetOrCreateReceivedAsync handles the concurrent-insert race from parallel topic delivery.
        var status = await _repo.GetOrCreateReceivedAsync(
            @event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue, ct);

        status.ApplyFinalized(@event.FinalStatus, @event.Reason, @event.PaymentOutcome, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected finalized {FinalStatus} for TrackingId {TrackingId}.",
            @event.FinalStatus, @event.TrackingId);
    }
}
