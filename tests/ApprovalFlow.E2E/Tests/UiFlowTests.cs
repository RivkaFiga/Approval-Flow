using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.E2E.Clients;
using ApprovalFlow.E2E.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApprovalFlow.E2E.Tests;

/// <summary>
/// Exercises the same Gateway endpoints the minimal UI (wwwroot/index.html) calls, covering the UI's three
/// user-facing capabilities end-to-end: submit an item, view its live status, and view the final decision.
/// </summary>
[Trait("Category", "E2E")]
public sealed class UiFlowTests
{
    private readonly E2ESettings _settings;
    private readonly GatewayClient _gateway;
    private readonly NotificationClient _notification;

    public UiFlowTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.E2E.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _settings     = config.GetSection("E2E").Get<E2ESettings>() ?? new E2ESettings();
        var jwt       = JwtTokenHelper.CreateSubmitterToken(_settings.Jwt);
        _gateway      = new GatewayClient(_settings.GatewayBaseUrl, jwt);
        _notification = new NotificationClient(_settings.NotificationBaseUrl);
    }

    [Fact]
    public async Task Gateway_ServesMinimalUiHomePage()
    {
        var html = await _gateway.GetHomePageHtmlAsync(CancellationToken.None);

        Assert.Contains("ApprovalFlow", html);
        Assert.Contains("id=\"submit-form\"", html);
    }

    [Fact]
    public async Task SubmitInvoice_FullUiFlow_PollsStatusThenReachesDecisionWithReason()
    {
        var fixtures = FixtureLoader.Load("sample-invoices.json");
        var fixture  = fixtures.First(f => f.Expected.Route == "auto_approve");
        var request  = FixtureLoader.ToRequest(fixture);

        // Step 1: submit the item (what the UI's "Submit" form does).
        var submitResponse = await _gateway.SubmitAsync(request, CancellationToken.None);
        Assert.False(
            string.IsNullOrWhiteSpace(submitResponse.TrackingId),
            "202 Accepted response must contain a non-empty TrackingId.");
        Assert.Equal(AcceptanceStatus.Accepted, submitResponse.Status);

        // Step 2: poll status (what the UI's live "Current status" view does).
        var finalStatus = await PollingHelper.WaitUntilAsync(
            ct => _notification.GetStatusAsync(submitResponse.TrackingId, ct),
            s  => IsTerminal(s.Status),
            TimeSpan.FromSeconds(_settings.FlowTimeoutSeconds),
            TimeSpan.FromMilliseconds(_settings.PollIntervalMs));

        // Step 3: verify the final decision (what the UI's "Final decision" section displays).
        Assert.Equal(LifecycleStatus.Paid, finalStatus.Status);
        Assert.Equal(Route.AutoApprove, finalStatus.Route);
        Assert.Equal(PaymentOutcome.Paid, finalStatus.PaymentOutcome);
        Assert.False(
            string.IsNullOrWhiteSpace(finalStatus.Reason),
            "A terminal decision must carry a plain-language reason for the submitter.");
        Assert.NotNull(finalStatus.AmountUsd);
    }

    private static bool IsTerminal(LifecycleStatus status) => status is
        LifecycleStatus.Paid          or
        LifecycleStatus.Rejected      or
        LifecycleStatus.PaymentFailed or
        LifecycleStatus.Duplicate;
}
