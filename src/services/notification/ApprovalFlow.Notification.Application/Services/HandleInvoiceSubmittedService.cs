using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Ingests <c>invoice.submitted</c> and records the initial <c>received</c> projection row (§5.2, F2).
/// Idempotent by <c>trackingId</c> — a redelivery for an already-tracked item is a no-op (§10).
/// </summary>
public sealed class HandleInvoiceSubmittedService
{
    private readonly ISubmissionStatusRepository _repo;
    private readonly ILogger<HandleInvoiceSubmittedService> _logger;

    public HandleInvoiceSubmittedService(
        ISubmissionStatusRepository repo,
        ILogger<HandleInvoiceSubmittedService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task HandleAsync(InvoiceSubmittedV1 @event, CancellationToken ct = default)
    {
        var existing = await _repo.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Submission already projected for TrackingId {TrackingId}; skipping redelivery.",
                @event.TrackingId);
            return;
        }

        var status = SubmissionStatus.CreateReceived(@event.TrackingId, @event.CorrelationId, @event.OccurredAt);
        await _repo.AddAsync(status, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected received for TrackingId {TrackingId}.", @event.TrackingId);
    }
}
