using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.E2E.Clients;
using ApprovalFlow.E2E.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApprovalFlow.E2E.Tests;

/// <summary>
/// Exercises the durable workflow path activated by <c>IApprovalWorkflowScheduler</c>: a
/// <c>human_review</c> item must appear in the approver queue (proving the workflow suspended on
/// <c>WaitForExternalEvent</c>), and calling <c>POST /approvals/{trackingId}/approve</c> must drive the
/// same instance to a terminal <c>Paid</c> status (proving the raised event resumed it).
/// </summary>
[Trait("Category", "E2E")]
public sealed class HumanReviewApprovalTests
{
    private readonly E2ESettings _settings;
    private readonly GatewayClient _gateway;
    private readonly NotificationClient _notification;
    private readonly ApprovalClient _approval;

    public HumanReviewApprovalTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.E2E.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _settings     = config.GetSection("E2E").Get<E2ESettings>() ?? new E2ESettings();
        _gateway      = new GatewayClient(_settings.GatewayBaseUrl);
        _notification = new NotificationClient(_settings.NotificationBaseUrl);
        _approval     = new ApprovalClient(_settings.ApprovalBaseUrl);
    }

    [Fact]
    public async Task HumanReviewInvoice_ApproveViaHitl_ReachesPaid()
    {
        var fixtures = FixtureLoader.Load("sample-invoices.json");
        var fixture  = fixtures.First(f => f.Expected.Route == "human_review");
        var request  = FixtureLoader.ToRequest(fixture);

        var submitResponse = await _gateway.SubmitAsync(request, CancellationToken.None);
        Assert.False(string.IsNullOrWhiteSpace(submitResponse.TrackingId));

        var trackingId = submitResponse.TrackingId;
        var flowTimeout = TimeSpan.FromSeconds(_settings.FlowTimeoutSeconds);
        var pollInterval = TimeSpan.FromMilliseconds(_settings.PollIntervalMs);

        var queue = await PollingHelper.WaitUntilAsync(
            async ct =>
            {
                var q = await _approval.GetQueueAsync(ct);
                return q?.Items.Any(i => i.TrackingId == trackingId) == true ? q : null;
            },
            _ => true,
            flowTimeout,
            pollInterval);

        Assert.Contains(queue.Items, i => i.TrackingId == trackingId);

        await _approval.ApproveAsync(trackingId, "e2e-approver", "looks good", CancellationToken.None);

        var finalStatus = await PollingHelper.WaitUntilAsync(
            ct => _notification.GetStatusAsync(trackingId, ct),
            s  => s.Status is LifecycleStatus.Paid
                            or LifecycleStatus.Rejected
                            or LifecycleStatus.PaymentFailed
                            or LifecycleStatus.Duplicate,
            flowTimeout,
            pollInterval);

        Assert.Equal(LifecycleStatus.Paid, finalStatus.Status);
    }
}
