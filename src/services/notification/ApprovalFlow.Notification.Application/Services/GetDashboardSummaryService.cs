using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Notification.Application.Ports;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Read-side of F8. Fetches raw aggregates from the projection store and maps them to
/// <see cref="DashboardSummaryResponse"/>, computing <c>EscalationRate</c> in-process.
/// </summary>
public sealed class GetDashboardSummaryService
{
    private readonly IDashboardRepository _repo;

    public GetDashboardSummaryService(IDashboardRepository repo) => _repo = repo;

    public async Task<DashboardSummaryResponse> GetAsync(CancellationToken ct = default)
    {
        var agg = await _repo.GetAggregateAsync(ct);

        var escalationRate = agg.TotalProcessed > 0
            ? (double)agg.HumanApprovalCount / agg.TotalProcessed
            : 0.0;

        return new DashboardSummaryResponse
        {
            TotalProcessed         = agg.TotalProcessed,
            AutoApprovedCount      = agg.AutoApprovedCount,
            HumanApprovalCount     = agg.HumanApprovalCount,
            EscalationRate         = escalationRate,
            AutoApprovedAmountUsd  = agg.AutoApprovedAmountUsd,
            HumanApprovedAmountUsd = agg.HumanApprovedAmountUsd
        };
    }
}
