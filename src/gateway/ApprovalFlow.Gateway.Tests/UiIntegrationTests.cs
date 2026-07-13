using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ApprovalFlow.Contracts.Enums;
using ApprovalFlow.Contracts.Invocation.V1;
using Dapr.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Gateway.Tests;

/// <summary>
/// Integration tests verifying the static UI page structure and the API flows it depends on.
/// HTML assertions confirm elements exist in the served page; auth assertions guard the endpoints
/// the JS calls against accidental exposure.
/// </summary>
public sealed class UiIntegrationTests : IClassFixture<GatewayFactory>
{
    private readonly GatewayFactory _factory;

    public UiIntegrationTests(GatewayFactory factory) => _factory = factory;

    // ── HTML structure ────────────────────────────────────────────────────────

    [Fact]
    public async Task IndexPage_Returns_200_With_Html()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<html", html);
    }

    [Fact]
    public async Task IndexPage_Contains_Submit_Form_Elements()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"submit-form\"", html);
        Assert.Contains("id=\"submit-button\"", html);
        Assert.Contains("id=\"submitter\"", html);
        Assert.Contains("id=\"invoiceNumber\"", html);
    }

    [Fact]
    public async Task IndexPage_Contains_Status_Lookup_Section()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"lookup-section\"", html);
        Assert.Contains("id=\"lookup-id\"", html);
        Assert.Contains("id=\"lookup-button\"", html);
        Assert.Contains("id=\"lookup-result\"", html);
        Assert.Contains("id=\"lookup-status\"", html);
        Assert.Contains("id=\"lookup-route\"", html);
        Assert.Contains("id=\"lookup-reason\"", html);
        Assert.Contains("id=\"lookup-amount\"", html);
        Assert.Contains("id=\"lookup-payment\"", html);
    }

    [Fact]
    public async Task IndexPage_Contains_Auto_Result_Sections()
    {
        var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");

        Assert.Contains("id=\"status-section\"", html);
        Assert.Contains("id=\"current-status\"", html);
        Assert.Contains("id=\"decision-section\"", html);
        Assert.Contains("id=\"decision-route\"", html);
        Assert.Contains("id=\"decision-reason\"", html);
    }

    // ── Status endpoint auth ──────────────────────────────────────────────────

    [Fact]
    public async Task StatusLookup_Unauthenticated_Returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/status/TRK-ANY-001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StatusLookup_Authenticated_Submitter_Passes_Auth()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue("test-submitter", "submitter"));

        var response = await client.GetAsync("/api/status/TRK-ANY-001");

        // Auth policy is "Authenticated" — submitter role satisfies it.
        // YARP will attempt to forward to the notification service which isn't running in
        // the test environment, so the response is a proxy error (4xx/5xx) — but NOT 401/403.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

/// <summary>
/// Tests for the full submit-invoice flow: DaprClient is configured to return a known response
/// so the controller's 202 Accepted body can be verified end-to-end.
/// Each test uses its own factory to keep mock state isolated.
/// </summary>
public sealed class SubmitFlowTests
{
    [Fact]
    public async Task Submit_Invoice_Returns_202_With_TrackingId()
    {
        await using var factory = new SubmitFlowFactory();
        const string expectedTrackingId = "TRK-UI-TEST-001";

        // InvokeMethodAsync<TRequest,TResponse> is a non-virtual concrete method on DaprClient;
        // mock the abstract overload it ultimately delegates to.
        factory.Dapr
            .InvokeMethodAsync<SubmitInvoiceResponse>(
                Arg.Any<HttpRequestMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubmitInvoiceResponse
            {
                TrackingId = expectedTrackingId,
                Status = AcceptanceStatus.Accepted
            }));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue("ui-submitter", "submitter"));

        var requestBody = new
        {
            invoice = new
            {
                invoiceNumber = "INV-UI-001",
                vendor = "Test Vendor",
                vendorKnown = true,
                submitter = "ui-user@example.com",
                department = "engineering-2026Q2",
                category = 1,
                currency = "USD",
                lineItems = new[] { new { description = "Test item", quantity = 1, unitPrice = 100.0 } },
                taxAmount = 0,
                total = 100.0,
                receiptPresent = true,
                date = "2026-07-13"
            }
        };

        var response = await client.PostAsJsonAsync("/api/intake", requestBody);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponseBody>();
        Assert.NotNull(body);
        Assert.Equal(expectedTrackingId, body.TrackingId);
    }

    [Fact]
    public async Task Submit_Invoice_Without_Auth_Returns_401()
    {
        await using var factory = new SubmitFlowFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/intake", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_Invoice_With_Approver_Role_Returns_403()
    {
        await using var factory = new SubmitFlowFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.Issue("approver-user", "approver"));

        var response = await client.PostAsJsonAsync("/api/intake", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record SubmitResponseBody(
        [property: JsonPropertyName("trackingId")] string TrackingId,
        [property: JsonPropertyName("status")] int Status);
}

public sealed class SubmitFlowFactory : WebApplicationFactory<Program>
{
    public DaprClient Dapr { get; } = Substitute.For<DaprClient>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DaprClient>();
            services.AddSingleton(Dapr);
        });
    }
}
