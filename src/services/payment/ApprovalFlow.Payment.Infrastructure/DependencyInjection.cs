using ApprovalFlow.Payment.Application.Ports;
using ApprovalFlow.Payment.Application.Services;
using ApprovalFlow.Payment.Infrastructure.Configuration;
using ApprovalFlow.Payment.Infrastructure.Persistence;
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

        services.AddScoped<IBudgetStore, DaprBudgetStore>();
        services.AddScoped<IPaymentIdempotencyStore, DaprPaymentIdempotencyStore>();

        services.AddScoped<ReserveBudgetService>();

        services.AddScoped<BudgetSeeder>();

        return services;
    }
}
