using ApprovalFlow.AiDecision.Domain.Entities;
using ApprovalFlow.AiDecision.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApprovalFlow.AiDecision.Tests;

public class DecisionRepositoryTests
{
    private static AiDecisionDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AiDecisionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AiDecisionDbContext(options);
    }

    [Fact]
    public async Task ExistsByTrackingId_returns_false_when_empty()
    {
        using var db = CreateInMemoryDb();
        var repo = new DecisionRepository(db);

        var exists = await repo.ExistsByTrackingIdAsync("TRK-none");

        Assert.False(exists);
    }

    [Fact]
    public async Task Add_and_find_by_tracking_id()
    {
        using var db = CreateInMemoryDb();
        var repo = new DecisionRepository(db);

        var decision = Decision.Create(
            trackingId: "TRK-1",
            correlationId: "corr-1",
            route: 0,
            recommendation: 0,
            confidence: 0.9,
            amountUsd: 99m,
            department: "engineering-2026Q2",
            citedRulesJson: "[]",
            fraudDetected: false,
            fraudReason: null,
            policyVersion: "policy-v1");

        await repo.AddAsync(decision);
        await repo.SaveChangesAsync();

        Assert.True(await repo.ExistsByTrackingIdAsync("TRK-1"));
    }
}
