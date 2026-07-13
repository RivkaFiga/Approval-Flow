using ApprovalFlow.Notification.Application.Ports;
using ApprovalFlow.Notification.Application.Services;
using ApprovalFlow.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("NotificationDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "notification")));

        services.AddScoped<ISubmissionStatusRepository, SubmissionStatusRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        services.AddScoped<HandleInvoiceSubmittedService>();
        services.AddScoped<HandleDecisionMadeService>();
        services.AddScoped<HandleReviewStatusService>();
        services.AddScoped<HandleItemFinalizedService>();
        services.AddScoped<HandlePaymentCompletedService>();
        services.AddScoped<GetSubmissionStatusService>();
        services.AddScoped<GetDashboardSummaryService>();

        return services;
    }
}
