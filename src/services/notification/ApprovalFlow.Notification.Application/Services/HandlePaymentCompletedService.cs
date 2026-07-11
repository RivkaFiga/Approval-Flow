using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Ingests <c>payment.completed</c> and advances the projection from <see cref="Contracts.Enums.LifecycleStatus.Paying"/>
/// to <see cref="Contracts.Enums.LifecycleStatus.Paid"/> or <see cref="Contracts.Enums.LifecycleStatus.PaymentFailed"/>
/// so <c>GET /status</c> reflects the actual payment outcome (§8, F2). Handles out-of-order arrival;
/// idempotent by monotonic <c>OccurredAt</c> (§10).
/// </summary>
public sealed class HandlePaymentCompletedService
{
    private readonly ISubmissionStatusRepository _repo;
    private readonly ILogger<HandlePaymentCompletedService> _logger;

    public HandlePaymentCompletedService(
        ISubmissionStatusRepository repo,
        ILogger<HandlePaymentCompletedService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentCompletedV1 @event, CancellationToken ct = default)
    {
        var status = await _repo.GetByTrackingIdAsync(@event.TrackingId, ct);
        if (status is null)
        {
            status = SubmissionStatus.CreateReceived(@event.TrackingId, @event.CorrelationId, DateTimeOffset.MinValue);
            await _repo.AddAsync(status, ct);
        }

        status.ApplyPaymentCompleted(@event.Outcome, @event.Reason, @event.OccurredAt);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Projected payment outcome {Outcome} for TrackingId {TrackingId}.",
            @event.Outcome, @event.TrackingId);
    }
}
