using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Ingests <c>review.status</c> and reflects the HITL sub-state on the projection so F2 shows a live status
/// during the slow 20% (§5.2, INV-1003). Handles out-of-order arrival by creating the row if missing.
/// Idempotent by monotonic <c>OccurredAt</c> (§10).
/// </summary>
public sealed class HandleReviewStatusService
{
    private readonly ISubmissionStatusRepository _repo;
    private readonly ILogger<HandleReviewStatusService> _logger;

    public HandleReviewStatusService(
        ISubmissionStatusRepository repo,
        ILogger<HandleReviewStatusService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task HandleAsync(ReviewStatusV1 @event, CancellationToken ct = default)
    {
        var status = await _repo.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (status is null)
        {
            // Out-of-order arrival: seed at MinValue so the event's OccurredAt advances state.
            status = SubmissionStatus.CreateReceived(@event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue);
            await _repo.AddAsync(status, ct);
        }

        status.ApplyReviewSubState(@event.SubState, @event.WhatWeStillNeed, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected review sub-state {SubState} for TrackingId {TrackingId}.",
            @event.SubState, @event.TrackingId);
    }
}
