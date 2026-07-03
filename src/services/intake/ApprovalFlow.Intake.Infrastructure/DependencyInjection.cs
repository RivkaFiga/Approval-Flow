using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Application.Services;
using ApprovalFlow.Intake.Infrastructure.Events;
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
        services.AddScoped<IIntakeEventPublisher, DaprIntakeEventPublisher>();
        services.AddScoped<IntakeService>();

        return services;
    }
}
