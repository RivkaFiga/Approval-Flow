using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ApprovalFlow.Notification.Infrastructure.Persistence;

public sealed class SubmissionStatusRepository : ISubmissionStatusRepository
{
    private readonly NotificationDbContext _db;

    public SubmissionStatusRepository(NotificationDbContext db) => _db = db;

    public Task<SubmissionStatus?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.SubmissionStatuses.FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct);

    public async Task<SubmissionStatus> GetOrCreateReceivedAsync(
        string trackingId, string correlationId, DateTimeOffset receivedAt, CancellationToken ct = default)
    {
        var existing = await _db.SubmissionStatuses.FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct);
        if (existing is not null)
            return existing;

        var entity = SubmissionStatus.CreateReceived(trackingId, correlationId, receivedAt);
        _db.SubmissionStatuses.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Two topic consumers raced to insert for the same TrackingId; discard the tracked
            // draft and return whichever row the other consumer committed.
            _db.ChangeTracker.Clear();
            return (await _db.SubmissionStatuses.FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct))!;
        }
    }

    public async Task AddAsync(SubmissionStatus status, CancellationToken ct = default)
        => await _db.SubmissionStatuses.AddAsync(status, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
