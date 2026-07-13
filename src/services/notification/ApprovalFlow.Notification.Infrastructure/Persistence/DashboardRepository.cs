using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace ApprovalFlow.Notification.Infrastructure.Persistence;

public sealed class DashboardRepository : IDashboardRepository
{
    private static readonly int AutoApproveRoute = (int)Route.AutoApprove;
    private static readonly int HumanReviewRoute = (int)Route.HumanReview;

    private readonly NotificationDbContext _db;

    public DashboardRepository(NotificationDbContext db) => _db = db;

    public async Task<DashboardAggregate> GetAggregateAsync(CancellationToken ct = default)
    {
        var set = _db.SubmissionStatuses;

        var total      = await set.CountAsync(x => x.Route != null, ct);
        var autoCount  = await set.CountAsync(x => x.Route == AutoApproveRoute, ct);
        var humanCount = await set.CountAsync(x => x.Route == HumanReviewRoute, ct);

        var autoAmount = await set
            .Where(x => x.Route == AutoApproveRoute)
            .SumAsync(x => (decimal?)x.AmountUsd, ct) ?? 0m;

        var humanAmount = await set
            .Where(x => x.Route == HumanReviewRoute)
            .SumAsync(x => (decimal?)x.AmountUsd, ct) ?? 0m;

        return new DashboardAggregate(total, autoCount, humanCount, autoAmount, humanAmount);
    }
}
