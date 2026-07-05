using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Notification.Infrastructure.Persistence;

public sealed class SubmissionStatusRepository : ISubmissionStatusRepository
{
    private readonly NotificationDbContext _db;

    public SubmissionStatusRepository(NotificationDbContext db) => _db = db;

    public Task<SubmissionStatus?> GetByTrackingIdAsync(string trackingId, CancellationToken ct = default)
        => _db.SubmissionStatuses.FirstOrDefaultAsync(x => x.TrackingId == trackingId, ct);

    public async Task AddAsync(SubmissionStatus status, CancellationToken ct = default)
        => await _db.SubmissionStatuses.AddAsync(status, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
