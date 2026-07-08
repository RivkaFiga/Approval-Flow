using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
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
        // Seed at MinValue so the event's own OccurredAt advances past the placeholder.
        // GetOrCreateReceivedAsync handles the concurrent-insert race from parallel topic delivery.
        var status = await _repo.GetOrCreateReceivedAsync(
            @event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue, ct);

        status.ApplyDecision(@event.Route, @event.AmountUsd, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected under-review for TrackingId {TrackingId} (route {Route}).",
            @event.TrackingId, @event.Route);
    }
}
