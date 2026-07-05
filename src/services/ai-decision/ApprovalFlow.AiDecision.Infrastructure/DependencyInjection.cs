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
        services.AddSingleton<IPolicySnapshotProvider, ConfigPolicySnapshotProvider>();
        services.AddSingleton<IPolicyAgent, StubPolicyAgent>();
        services.AddScoped<DecideInvoiceService>();

        return services;
    }
}
