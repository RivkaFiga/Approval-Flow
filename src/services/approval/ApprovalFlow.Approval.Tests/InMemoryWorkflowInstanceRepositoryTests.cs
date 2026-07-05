using ApprovalFlow.Approval.Domain.Entities;
using ApprovalFlow.Approval.Domain.Values;
using ApprovalFlow.Approval.Infrastructure.Persistence;
using ApprovalFlow.Contracts.Enums;
using Xunit;

namespace ApprovalFlow.Approval.Tests;

public class InMemoryWorkflowInstanceRepositoryTests
{
    private static WorkflowInstance NewInstance(string trackingId) => WorkflowInstance.Create(
        trackingId,
        correlationId: "corr-1",
        route: Route.AutoApprove,
        state: WorkflowState.AutoApproved,
        recommendation: Recommendation.Approve,
        confidence: 0.9,
        amountUsd: 100m,
        department: "engineering-2026Q2",
        citedRulesJson: "[]",
        fraudDetected: false,
        fraudReason: null);

    [Fact]
    public async Task Add_then_SaveChanges_makes_the_instance_visible()
    {
        var repo = new InMemoryWorkflowInstanceRepository();

        Assert.False(await repo.ExistsByTrackingIdAsync("TRK-1"));

        await repo.AddAsync(NewInstance("TRK-1"));
        Assert.False(await repo.ExistsByTrackingIdAsync("TRK-1"));

        await repo.SaveChangesAsync();
        Assert.True(await repo.ExistsByTrackingIdAsync("TRK-1"));
    }

    [Fact]
    public async Task First_writer_wins_per_tracking_id()
    {
        var repo = new InMemoryWorkflowInstanceRepository();

        await repo.AddAsync(NewInstance("TRK-1"));
        await repo.SaveChangesAsync();

        await repo.AddAsync(NewInstance("TRK-1"));
        await repo.SaveChangesAsync();

        Assert.True(await repo.ExistsByTrackingIdAsync("TRK-1"));
    }
}
