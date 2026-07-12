using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.AiDecision.Application.Services;
using ApprovalFlow.AiDecision.Infrastructure.Agents;
using ApprovalFlow.AiDecision.Infrastructure.Events;
using ApprovalFlow.AiDecision.Infrastructure.Persistence;
using ApprovalFlow.AiDecision.Infrastructure.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.AiDecision.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiDecisionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AiDecisionDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("AiDecisionDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ai_decision")));

        services.Configure<PolicySnapshotOptions>(
            configuration.GetSection(PolicySnapshotOptions.SectionName));

        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<IDecisionEventPublisher, DaprDecisionEventPublisher>();
        services.AddSingleton<ConfigPolicySnapshotProvider>();
        services.AddSingleton<DaprConfigPolicySnapshotProvider>();
        services.AddSingleton<IPolicySnapshotProvider>(sp =>
            sp.GetRequiredService<DaprConfigPolicySnapshotProvider>());
        services.AddSingleton<IPolicySnapshotRefresher>(sp =>
            sp.GetRequiredService<DaprConfigPolicySnapshotProvider>());
        services.AddScoped<DecideInvoiceService>();

        RegisterPolicyAgent(services, configuration);

        return services;
    }

    private static void RegisterPolicyAgent(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));

        var geminiSection = configuration.GetSection(GeminiOptions.SectionName);
        var useStub = geminiSection.GetValue("UseStub", defaultValue: true);
        var hasApiKey = !string.IsNullOrWhiteSpace(geminiSection["ApiKey"]);

        if (useStub)
        {
            services.AddSingleton<IPolicyAgent, StubPolicyAgent>();
            return;
        }

        if (!hasApiKey)
        {
            throw new InvalidOperationException(
                "Gemini:UseStub is false but Gemini:ApiKey is not configured. " +
                "Set the GEMINI__APIKEY environment variable or add Gemini:ApiKey to appsettings.");
        }

        services.AddHttpClient(GeminiPolicyAgent.HttpClientName);
        services.AddSingleton<IPolicyAgent, GeminiPolicyAgent>();
    }
}
