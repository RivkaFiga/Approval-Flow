using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.E2E.Clients;
using ApprovalFlow.E2E.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApprovalFlow.E2E.Tests;

[Trait("Category", "E2E")]
public sealed class InvoiceFlowTests
{
    private readonly E2ESettings _settings;
    private readonly GatewayClient _gateway;
    private readonly NotificationClient _notification;

    public InvoiceFlowTests()
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
    public async Task Gateway_HealthCheck_ReturnsHealthy()
    {
        var timeout  = TimeSpan.FromSeconds(_settings.HealthTimeoutSeconds);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            using var probeCt = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            if (await _gateway.IsHealthyAsync(probeCt.Token))
                return;

            await Task.Delay(1_000);
        }

        Assert.Fail($"Gateway /healthz did not return healthy within {_settings.HealthTimeoutSeconds}s.");
    }

    [Fact]
    public async Task AutoApproveInvoice_FullFlow_ReachesTerminalState()
    {
        var fixtures = FixtureLoader.Load("sample-invoices.json");
        var fixture  = fixtures.First(f => f.Expected.Route == "auto_approve");
        var request  = FixtureLoader.ToRequest(fixture);

        var submitResponse = await _gateway.SubmitAsync(request, CancellationToken.None);

        Assert.False(
            string.IsNullOrWhiteSpace(submitResponse.TrackingId),
            "202 Accepted response must contain a non-empty TrackingId.");

        var finalStatus = await PollingHelper.WaitUntilAsync(
            ct => _notification.GetStatusAsync(submitResponse.TrackingId, ct),
            s  => IsTerminal(s.Status),
            TimeSpan.FromSeconds(_settings.FlowTimeoutSeconds),
            TimeSpan.FromMilliseconds(_settings.PollIntervalMs));

        Assert.True(
            IsTerminal(finalStatus.Status),
            $"Fixture {fixture.Id}: expected terminal state (Paid/Rejected/PaymentFailed/Duplicate), got {finalStatus.Status}.");
    }

    // LifecycleStatus.Paid       → PaymentCompleted
    // Rejected | PaymentFailed   → Compensated
    // Duplicate                  → short-circuit, no payment
    private static bool IsTerminal(LifecycleStatus status) => status is
        LifecycleStatus.Paid          or
        LifecycleStatus.Rejected      or
        LifecycleStatus.PaymentFailed or
        LifecycleStatus.Duplicate;
}
