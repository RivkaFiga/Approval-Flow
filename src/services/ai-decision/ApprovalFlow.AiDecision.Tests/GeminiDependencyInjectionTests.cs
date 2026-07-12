using ApprovalFlow.AiDecision.Infrastructure;
using ApprovalFlow.AiDecision.Infrastructure.Agents;
using ApprovalFlow.AiDecision.Infrastructure.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class GeminiDependencyInjectionTests
{
    private static IConfiguration BuildConfig(string useStub, string apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{GeminiOptions.SectionName}:UseStub"]            = useStub,
                [$"{GeminiOptions.SectionName}:ApiKey"]             = apiKey,
                [$"{GeminiOptions.SectionName}:TimeoutSeconds"]     = "30",
                [$"{PolicySnapshotOptions.SectionName}:Version"]    = "test-policy",
                ["ConnectionStrings:AiDecisionDb"]                   = "Host=localhost;Database=test"
            })
            .Build();

    [Fact]
    public void AddAiDecisionInfrastructure_WhenUseStubTrue_RegistersWithoutThrowing()
    {
        var config = BuildConfig(useStub: "true", apiKey: string.Empty);
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAiDecisionInfrastructure(config); // must not throw
    }

    [Fact]
    public void AddAiDecisionInfrastructure_WhenUseStubFalseAndApiKeySet_RegistersWithoutThrowing()
    {
        var config = BuildConfig(useStub: "false", apiKey: "real-api-key");
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAiDecisionInfrastructure(config); // must not throw
    }

    [Fact]
    public void AddAiDecisionInfrastructure_WhenUseStubFalseAndApiKeyEmpty_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(useStub: "false", apiKey: string.Empty);
        var services = new ServiceCollection();
        services.AddLogging();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddAiDecisionInfrastructure(config));

        Assert.Contains("Gemini:ApiKey", ex.Message);
        Assert.Contains("GEMINI__APIKEY", ex.Message);
    }

    [Fact]
    public void AddAiDecisionInfrastructure_WhenUseStubFalseAndApiKeyWhitespace_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(useStub: "false", apiKey: "   ");
        var services = new ServiceCollection();
        services.AddLogging();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAiDecisionInfrastructure(config));
    }
}
