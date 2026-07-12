using System.Net;
using System.Text;
using System.Text.Json;
using ApprovalFlow.AiDecision.Infrastructure.Agents;
using ApprovalFlow.Contracts.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class GeminiPolicyAgentTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static GeminiPolicyAgent CreateSut(
        string? responseBody = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Exception? throwException = null)
    {
        var handler = new FakeHttpHandler(responseBody, statusCode, throwException);
        var httpClient = new HttpClient(handler);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(GeminiPolicyAgent.HttpClientName).Returns(httpClient);

        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "gemini-1.5-flash",
            TimeoutSeconds = 5,
            UseStub = false
        });

        return new GeminiPolicyAgent(factory, options, NullLogger<GeminiPolicyAgent>.Instance);
    }

    /// <summary>Wraps a recommendation DTO in the Gemini REST response envelope.</summary>
    private static string GeminiEnvelope(object inner) =>
        JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        role = "model",
                        parts = new[] { new { text = JsonSerializer.Serialize(inner) } }
                    }
                }
            }
        });

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RecommendAsync_WithApproveResponse_ReturnsMappedRecommendation()
    {
        var body = GeminiEnvelope(new
        {
            recommendation = "Approve",
            confidence = 0.92,
            fraudDetected = false,
            fraudReason = (string?)null,
            policyViolations = Array.Empty<object>()
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(Recommendation.Approve, result.Recommendation);
        Assert.Equal(0.92, result.Confidence, precision: 5);
        Assert.Null(result.FraudSignal);
        Assert.Empty(result.PolicyViolations);
    }

    [Fact]
    public async Task RecommendAsync_WithRejectResponse_ReturnsRejectRecommendation()
    {
        var body = GeminiEnvelope(new
        {
            recommendation = "Reject",
            confidence = 0.95,
            fraudDetected = false,
            fraudReason = (string?)null,
            policyViolations = new[] { new { ruleId = "MEAL-03", detail = "Alcohol detected" } }
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Meals(50m, 2), Fixtures.DefaultPolicy());

        Assert.Equal(Recommendation.Reject, result.Recommendation);
        Assert.Equal(0.95, result.Confidence, precision: 5);
        Assert.Single(result.PolicyViolations);
        Assert.Equal("MEAL-03", result.PolicyViolations[0].RuleId);
    }

    [Fact]
    public async Task RecommendAsync_WithFraudDetected_ReturnsFraudSignal()
    {
        var body = GeminiEnvelope(new
        {
            recommendation = "Reject",
            confidence = 0.88,
            fraudDetected = true,
            fraudReason = "Duplicate submission detected",
            policyViolations = Array.Empty<object>()
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(50m), Fixtures.DefaultPolicy());

        Assert.NotNull(result.FraudSignal);
        Assert.True(result.FraudSignal!.Detected);
        Assert.Equal("Duplicate submission detected", result.FraudSignal.Reason);
    }

    [Fact]
    public async Task RecommendAsync_WithEscalateResponse_ReturnsRejectRecommendation()
    {
        // "Escalate" is not a Recommendation enum value; the agent maps it to Reject
        // so the router sends to HumanReview.
        var body = GeminiEnvelope(new
        {
            recommendation = "Escalate",
            confidence = 0.55,
            fraudDetected = false,
            fraudReason = (string?)null,
            policyViolations = Array.Empty<object>()
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Travel(200m), Fixtures.DefaultPolicy());

        Assert.Equal(Recommendation.Reject, result.Recommendation);
    }

    [Fact]
    public async Task RecommendAsync_WhenApiFails_ReturnsNoConfidence()
    {
        var sut = CreateSut(statusCode: HttpStatusCode.InternalServerError);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
        Assert.Null(result.FraudSignal);
    }

    [Fact]
    public async Task RecommendAsync_WhenHttpRequestThrows_ReturnsNoConfidence()
    {
        var sut = CreateSut(throwException: new HttpRequestException("Connection refused"));
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task RecommendAsync_WhenTimeout_ReturnsNoConfidence()
    {
        // Simulate timeout by throwing TaskCanceledException (subtype of OperationCanceledException)
        // with a token that is NOT the caller's token, mimicking a CancelAfter firing.
        var sut = CreateSut(throwException: new TaskCanceledException("Timeout simulated"));
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task RecommendAsync_WhenOuterJsonInvalid_ReturnsNoConfidence()
    {
        var sut = CreateSut("not-valid-json");
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task RecommendAsync_WhenInnerJsonInvalid_ReturnsNoConfidence()
    {
        // Valid outer envelope, but the text content is not valid recommendation JSON.
        var body = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "not a json object at all" } }
                    }
                }
            }
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task RecommendAsync_WhenGeminiReturnsEmptyCandidates_ReturnsNoConfidence()
    {
        var body = JsonSerializer.Serialize(new { candidates = Array.Empty<object>() });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task RecommendAsync_ConfidenceIsClamped_WhenGeminiReturnsOutOfRange()
    {
        var body = GeminiEnvelope(new
        {
            recommendation = "Approve",
            confidence = 1.5,   // > 1.0 — must be clamped to 1.0
            fraudDetected = false,
            fraudReason = (string?)null,
            policyViolations = Array.Empty<object>()
        });

        var sut = CreateSut(body);
        var result = await sut.RecommendAsync(
            Fixtures.Saas(100m), Fixtures.DefaultPolicy());

        Assert.Equal(1.0, result.Confidence);
    }

    // ── fake HTTP infrastructure ──────────────────────────────────────────────

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string? _body;
        private readonly HttpStatusCode _status;
        private readonly Exception? _throw;

        public FakeHttpHandler(string? body, HttpStatusCode status, Exception? @throw)
        {
            _body = body;
            _status = status;
            _throw = @throw;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_throw is not null)
                throw _throw;

            var response = new HttpResponseMessage(_status)
            {
                Content = _body is not null
                    ? new StringContent(_body, Encoding.UTF8, "application/json")
                    : new StringContent(string.Empty)
            };
            return Task.FromResult(response);
        }
    }
}
