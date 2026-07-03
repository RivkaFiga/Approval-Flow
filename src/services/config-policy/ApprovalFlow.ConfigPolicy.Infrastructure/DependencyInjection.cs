using ApprovalFlow.ConfigPolicy.Application.Ports;
using ApprovalFlow.ConfigPolicy.Application.Services;
using ApprovalFlow.ConfigPolicy.Infrastructure.Events;
using ApprovalFlow.ConfigPolicy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.ConfigPolicy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddConfigPolicyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ConfigPolicyDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ConfigPolicyDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "config_policy")));

        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IPolicyEventPublisher, DaprPolicyEventPublisher>();
        services.AddScoped<PolicyService>();

        return services;
    }
}
