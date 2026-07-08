using ApprovalFlow.Notification.Domain.Entities;

namespace ApprovalFlow.Notification.Application.Ports;

/// <summary>
/// Persistence port for the live status projection (§11). A first-writer-wins insert on the tracking id
/// (backed by a UNIQUE constraint in the durable adapter) is what makes the <c>invoice.submitted</c> handler
/// safe under redelivery.
/// </summary>
public interface ISubmissionStatusRepository
{
    Task<SubmissionStatus?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default);

    /// <summary>
    /// Returns the existing row for <paramref name="trackingId"/> if one exists, otherwise inserts a
    /// <see cref="SubmissionStatus.CreateReceived"/> placeholder and returns it. Safe under concurrent
    /// delivery of multiple topics for the same tracking id (handles the 23505 race at the adapter level).
    /// </summary>
    Task<SubmissionStatus> GetOrCreateReceivedAsync(string trackingId, string correlationId, DateTimeOffset receivedAt, CancellationToken ct = default);

    Task AddAsync(SubmissionStatus status, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
