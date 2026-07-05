using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Ingests <c>decision.made</c> and advances the projection to <c>under-review</c>, recording the router's
/// route and USD amount (§5.2, F2). Handles out-of-order arrival: if <c>invoice.submitted</c> hasn't been
/// seen yet, the projection is created here at the event's <c>OccurredAt</c>. Idempotent by monotonic
/// <c>OccurredAt</c> (§10).
/// </summary>
public sealed class HandleDecisionMadeService
{
    private readonly ISubmissionStatusRepository _repo;
    private readonly ILogger<HandleDecisionMadeService> _logger;

    public HandleDecisionMadeService(
        ISubmissionStatusRepository repo,
        ILogger<HandleDecisionMadeService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task HandleAsync(DecisionMadeV1 @event, CancellationToken ct = default)
    {
        var status = await _repo.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (status is null)
        {
            // Out-of-order arrival: the row is seeded at MinValue so the event's own OccurredAt still
            // advances the projection past the synthesized <c>received</c> placeholder.
            status = SubmissionStatus.CreateReceived(@event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue);
            await _repo.AddAsync(status, ct);
        }

        status.ApplyDecision(@event.Route, @event.AmountUsd, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected under-review for TrackingId {TrackingId} (route {Route}).",
            @event.TrackingId, @event.Route);
    }
}
