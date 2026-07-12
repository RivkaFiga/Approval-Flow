using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.E2E.Clients;
using ApprovalFlow.E2E.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApprovalFlow.E2E.Tests;

/// <summary>
/// Verifies the M-milestone hot-reload wiring: publishing a policy through Config/Policy raises
/// <c>policy.changed</c>, AI-Decision drops its cached snapshot, and the very next decision applies the
/// new thresholds — no service restart. The test first pins a permissive policy and confirms a small
/// invoice auto-approves, then tightens the autonomy ceiling below the same invoice's amount and
/// confirms subsequent submissions escalate to human review.
/// </summary>
[Trait("Category", "E2E")]
public sealed class PolicyHotReloadTests
{
    private readonly E2ESettings _settings;
    private readonly GatewayClient _gateway;
    private readonly NotificationClient _notification;
    private readonly ConfigPolicyClient _config;

    public PolicyHotReloadTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.E2E.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _settings     = config.GetSection("E2E").Get<E2ESettings>() ?? new E2ESettings();
        var jwt       = JwtTokenHelper.CreateSubmitterToken(_settings.Jwt);
        _gateway      = new GatewayClient(_settings.GatewayBaseUrl, jwt);
        _notification = new NotificationClient(_settings.NotificationBaseUrl);
        _config       = new ConfigPolicyClient(_settings.ConfigPolicyBaseUrl);
    }

    [Fact]
    public async Task UpdatingPolicy_AtRuntime_ChangesRouteForSubsequentInvoices()
    {
        var ct = CancellationToken.None;

        // ── 0. Wait for Config/Policy to come up ─────────────────────────
        await PollingHelper.WaitUntilAsync(
            async token => await _config.IsHealthyAsync(token) ? "up" : null,
            _ => true,
            TimeSpan.FromSeconds(_settings.HealthTimeoutSeconds),
            TimeSpan.FromSeconds(1),
            ct);

        // ── 1. Publish a permissive policy: $250 ceiling, our vendor known ──
        var permissive = await _config.CreateAsync(new CreatePolicyBody(
            Name: $"hot-reload-permissive-{Guid.NewGuid():N}",
            Markdown: "# E2E hot-reload permissive policy",
            AutonomyCeilingUsd: 250m,
            AutonomyMinConfidence: 0.80,
            BaseCurrency: "USD",
            FxRates: new Dictionary<string, decimal> { ["EUR"] = 1.08m, ["GBP"] = 1.27m },
            KnownVendors: new List<string> { "Bistro 19", "Atlassian", "Dell", "The Rooftop Grill", "Trattoria Verde" }
        ), ct);

        // ── 2. Under the permissive policy an in-policy invoice must auto-approve ──
        var underCeilingInvoice = BuildInvoiceRequest(totalUsd: 42m);
        var firstRoute = await SubmitAndAwaitRouteAsync(underCeilingInvoice, ct);
        Assert.Equal(Route.AutoApprove, firstRoute);

        // ── 3. Tighten the policy — same active document, new autonomy ceiling ──
        // Slashing the ceiling to $5 makes the same $42 invoice ineligible for
        // autonomy; the router must escalate to human_review.
        await _config.UpdateAsync(permissive.Id, new UpdatePolicyBody(
            Name: permissive.Version > 0 ? "hot-reload-strict" : "hot-reload-strict",
            Markdown: "# E2E hot-reload strict policy",
            AutonomyCeilingUsd: 5m,
            AutonomyMinConfidence: 0.80,
            BaseCurrency: "USD",
            FxRates: new Dictionary<string, decimal> { ["EUR"] = 1.08m, ["GBP"] = 1.27m },
            KnownVendors: new List<string> { "Bistro 19", "Atlassian", "Dell", "The Rooftop Grill", "Trattoria Verde" },
            ExpectedVersion: permissive.Version
        ), ct);

        // ── 4. Poll: submit fresh invoices until AI-Decision reflects the new policy ──
        // policy.changed is async — the subscriber invalidates the cache, and the very next
        // GetAsync from Config/Policy pulls the tightened snapshot. Retrying a handful of
        // submissions absorbs the delivery race without introducing a hard sleep.
        var flippedRoute = await PollUntilRouteAsync(
            expected: Route.HumanReview,
            buildRequest: () => BuildInvoiceRequest(totalUsd: 42m),
            timeout: TimeSpan.FromSeconds(_settings.FlowTimeoutSeconds),
            attemptInterval: TimeSpan.FromSeconds(2),
            ct: ct);

        Assert.Equal(Route.HumanReview, flippedRoute);
    }

    private async Task<Route> SubmitAndAwaitRouteAsync(SubmitInvoiceRequest request, CancellationToken ct)
    {
        var submitResponse = await _gateway.SubmitAsync(request, ct);
        Assert.False(string.IsNullOrWhiteSpace(submitResponse.TrackingId));

        var status = await PollingHelper.WaitUntilAsync(
            token => _notification.GetStatusAsync(submitResponse.TrackingId, token),
            s => s.Route is not null,
            TimeSpan.FromSeconds(_settings.FlowTimeoutSeconds),
            TimeSpan.FromMilliseconds(_settings.PollIntervalMs),
            ct);

        return status.Route!.Value;
    }

    private async Task<Route> PollUntilRouteAsync(
        Route expected,
        Func<SubmitInvoiceRequest> buildRequest,
        TimeSpan timeout,
        TimeSpan attemptInterval,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        Route lastObserved = default;
        var attempt = 0;

        while (DateTime.UtcNow < deadline)
        {
            attempt++;
            lastObserved = await SubmitAndAwaitRouteAsync(buildRequest(), ct);
            if (lastObserved == expected)
                return lastObserved;

            await Task.Delay(attemptInterval, ct);
        }

        throw new TimeoutException(
            $"Expected route {expected} after policy update but observed {lastObserved} across {attempt} attempt(s).");
    }

    private static SubmitInvoiceRequest BuildInvoiceRequest(decimal totalUsd)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return new SubmitInvoiceRequest
        {
            Invoice = new Invoice
            {
                InvoiceNumber  = $"HOTRELOAD-{suffix}",
                Vendor         = "Bistro 19",
                VendorKnown    = true,
                Submitter      = "e2e-hotreload@northwind.example",
                Department     = "engineering-2026Q2",
                Category       = ExpenseCategory.Meals,
                Currency       = "USD",
                LineItems      = new List<LineItem>
                {
                    new() { Description = "Team lunch", Quantity = 1, UnitPrice = totalUsd }
                },
                TaxAmount      = 0m,
                Total          = totalUsd,
                ReceiptPresent = true,
                Attendees      = 1,
                Date           = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                Notes          = "E2E policy hot-reload probe"
            }
        };
    }
}
