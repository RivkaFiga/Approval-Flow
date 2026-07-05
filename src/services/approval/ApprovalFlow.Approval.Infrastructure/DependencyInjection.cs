using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Application.Services;
using ApprovalFlow.Approval.Infrastructure.Events;
using ApprovalFlow.Approval.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Approval.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApprovalInfrastructure(this IServiceCollection services)
    {
        // Singleton store — process-lifetime in-memory persistence for this slice; swappable to a durable
        // adapter (Dapr Workflow state + queryable Postgres projection, §9.1, §11) without touching callers.
        services.AddSingleton<IWorkflowInstanceRepository, InMemoryWorkflowInstanceRepository>();
        services.AddScoped<IWorkflowEventPublisher, DaprWorkflowEventPublisher>();
        services.AddScoped<HandleDecisionMadeService>();

        return services;
    }
}
