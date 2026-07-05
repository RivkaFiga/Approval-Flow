using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Application.Services;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using ApprovalFlow.Payment.Infrastructure.Persistence;
using ApprovalFlow.Payment.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<PaymentInfrastructureOptions>()
            .Bind(configuration.GetSection(PaymentInfrastructureOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<SimulatedPaymentProviderOptions>()
            .Bind(configuration.GetSection("Payment:Provider"))
            .ValidateOnStart();

        // Postgres-backed append-only ledger (§8, §11). Per-service schema mirrors Intake/Notification.
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PaymentDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "payment")));

        // Reserve-step ports (unchanged from the earlier slice — do not touch behavior).
        services.AddScoped<IBudgetStore, DaprBudgetStore>();
        services.AddScoped<IPaymentIdempotencyStore, DaprPaymentIdempotencyStore>();
        services.AddScoped<ReserveBudgetService>();
        services.AddScoped<BudgetSeeder>();

        // Execute-step ports.
        services.AddScoped<IPaymentLedgerRepository, PaymentLedgerRepository>();
        services.AddSingleton<IPaymentProvider, SimulatedPaymentProvider>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ExecutePaymentService>();

        return services;
    }
}
