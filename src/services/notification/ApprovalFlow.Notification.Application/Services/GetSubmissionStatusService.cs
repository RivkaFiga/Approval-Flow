using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Notification.Application.Ports;

namespace ApprovalFlow.Notification.Application.Services;

/// <summary>
/// Read-side of F2. Resolves a <see cref="SubmissionStatusResponse"/> from the live projection for the
/// given tracking id, or <c>null</c> if the submission is unknown.
/// </summary>
public sealed class GetSubmissionStatusService
{
    private readonly ISubmissionStatusRepository _repo;

    public GetSubmissionStatusService(ISubmissionStatusRepository repo) => _repo = repo;

    public async Task<SubmissionStatusResponse?> GetAsync(string trackingId, CancellationToken ct = default)
    {
        var status = await _repo.GetByTrackingIdAsync(trackingId, ct);
        if (status is null) return null;

        return new SubmissionStatusResponse
        {
            TrackingId = status.TrackingId,
            Status = status.CurrentStatus,
            Route = status.CurrentRoute,
            AmountUsd = status.AmountUsd,
            PaymentOutcome = status.CurrentPaymentOutcome,
            Reason = status.Reason,
            UpdatedAt = status.UpdatedAt
        };
    }
}
