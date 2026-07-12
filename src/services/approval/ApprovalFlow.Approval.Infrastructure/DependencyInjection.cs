using ApprovalFlow.Approval.Application.Ports;
using ApprovalFlow.Approval.Infrastructure.Events;
using ApprovalFlow.Approval.Infrastructure.Persistence;
using ApprovalFlow.Approval.Infrastructure.Workflows;
using ApprovalFlow.Approval.Infrastructure.Workflows.Activities;
using Dapr.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Approval.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApprovalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApprovalDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ApprovalDb"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "approval")));

        services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.AddScoped<IPendingApprovalRepository, PendingApprovalRepository>();
        services.AddScoped<IWorkflowEventPublisher, DaprWorkflowEventPublisher>();

        services.AddScoped<IApprovalWorkflowScheduler, DaprApprovalWorkflowScheduler>();
        services.AddScoped<IApprovalWorkflowEventRaiser, DaprApprovalWorkflowEventRaiser>();

        services.AddDaprWorkflow(options =>
        {
            options.RegisterWorkflow<ApprovalWorkflow>();
            options.RegisterActivity<PersistInstanceActivity>();
            options.RegisterActivity<EnqueuePendingApprovalActivity>();
            options.RegisterActivity<ResolvePendingApprovalActivity>();
            options.RegisterActivity<PublishReviewStatusActivity>();
            options.RegisterActivity<PublishItemFinalizedActivity>();
        });

        return services;
    }
}
