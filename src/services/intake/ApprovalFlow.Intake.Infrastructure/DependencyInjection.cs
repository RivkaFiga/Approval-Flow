using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Application.Services;
using ApprovalFlow.Intake.Infrastructure.Events;
using ApprovalFlow.Intake.Infrastructure.Outbox;
using ApprovalFlow.Intake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Intake.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIntakeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IntakeDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("IntakeDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "intake")));

        services.AddScoped<ISubmittedInvoiceRepository, SubmittedInvoiceRepository>();

        // IntakeService now stages events into the Outbox instead of publishing to Dapr.
        services.AddScoped<IIntakeEventPublisher, OutboxIntakeEventPublisher>();

        // Outbox dispatch pipeline.
        services.AddScoped<IOutboxDispatchStore, OutboxDispatchStore>();
        services.AddScoped<IEventBusPublisher, DaprEventBusPublisher>();
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxDispatcher>();

        services.AddScoped<IntakeService>();

        return services;
    }
}
