using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApprovalFlow.E2E.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ApprovalFlow.E2E.Tests;

/// <summary>
/// Verifies Gateway-level security: unauthenticated requests are rejected, valid JWT requests
/// pass authentication and reach backend services, and the health endpoint stays anonymous.
/// Requires a running stack (docker compose up or equivalent).
/// </summary>
[Trait("Category", "E2E")]
public sealed class GatewaySecurityTests
{
    private readonly E2ESettings _settings;

    public GatewaySecurityTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.E2E.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _settings = config.GetSection("E2E").Get<E2ESettings>() ?? new E2ESettings();
    }

    // ── Unauthenticated rejections ──────────────────────────────────────────

    public static IEnumerable<object[]> ProtectedRoutes => new[]
    {
        new object[] { HttpMethod.Post, "/api/intake",        true  },
        new object[] { HttpMethod.Get,  "/api/status/any-id", false },
        new object[] { HttpMethod.Get,  "/approvals/queue",   false },
    };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Unauthenticated_Request_Returns_401(HttpMethod method, string path, bool sendBody)
    {
        using var client = AnonymousClient();
        using var request = new HttpRequestMessage(method, path);
        if (sendBody)
            request.Content = JsonContent.Create(new { });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Authenticated routing ───────────────────────────────────────────────

    [Fact]
    public async Task Authenticated_StatusRoute_ReachesBackend_NotUnauthorized()
    {
        // A non-existent trackingId should return 404 from notification, not 401/403,
        // proving auth passed and YARP routed to the notification service.
        using var client = AuthenticatedClient(sub: "e2e-sec-u1", roles: "submitter");

        var response = await client.GetAsync("/api/status/e2e-nonexistent-id");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden,    response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_Submitter_Cannot_Access_Approvals_Queue()
    {
        using var client = AuthenticatedClient(sub: "e2e-sec-u2", roles: "submitter");

        var response = await client.GetAsync("/approvals/queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Health endpoint stays anonymous ────────────────────────────────────

    [Fact]
    public async Task Health_Endpoint_Does_Not_Require_Authentication()
    {
        using var client = AnonymousClient();

        var response = await client.GetAsync("/healthz");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.IsSuccessStatusCode, $"/healthz returned {(int)response.StatusCode}");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private HttpClient AnonymousClient() =>
        new() { BaseAddress = new Uri(_settings.GatewayBaseUrl) };

    private HttpClient AuthenticatedClient(string sub, params string[] roles)
    {
        var token = JwtTokenHelper.CreateToken(sub, roles, _settings.Jwt);
        var client = new HttpClient { BaseAddress = new Uri(_settings.GatewayBaseUrl) };
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
