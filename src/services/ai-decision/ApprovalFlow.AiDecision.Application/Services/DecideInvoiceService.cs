using System.Text.Json;
using ApprovalFlow.AiDecision.Application.Ports;
using ApprovalFlow.AiDecision.Domain.Entities;
using ApprovalFlow.AiDecision.Domain.Rules;
using ApprovalFlow.AiDecision.Domain.Values;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.AiDecision.Application.Services;

/// <summary>
/// Orchestrates one <c>invoice.submitted</c> → <c>decision.made</c> pass (§7): load policy, run pre-checks,
/// run CategoryRules, ask the agent, run the deterministic router, persist the audit record, publish the
/// event. Idempotent by <c>trackingId</c> — a redelivered <c>invoice.submitted</c> for an already-decided
/// item is a no-op (§10, redelivery de-dup layer for this stage).
/// </summary>
public sealed class DecideInvoiceService
{
    private readonly IPolicySnapshotProvider _policyProvider;
    private readonly IPolicyAgent _agent;
    private readonly IDecisionRepository _repo;
    private readonly IDecisionEventPublisher _publisher;
    private readonly ILogger<DecideInvoiceService> _logger;

    public DecideInvoiceService(
        IPolicySnapshotProvider policyProvider,
        IPolicyAgent agent,
        IDecisionRepository repo,
        IDecisionEventPublisher publisher,
        ILogger<DecideInvoiceService> logger)
    {
        _policyProvider = policyProvider;
        _agent = agent;
        _repo = repo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DecideAsync(InvoiceSubmittedV1 @event, CancellationToken ct = default)
    {
        if (await _repo.ExistsByTrackingIdAsync(@event.TrackingId, ct))
        {
            _logger.LogInformation(
                "Decision already recorded for TrackingId {TrackingId}; skipping redelivery.",
                @event.TrackingId);
            return;
        }

        var policy = await _policyProvider.GetAsync(ct);
        var preChecks = PreCheckEvaluator.Evaluate(@event.Invoice, policy);
        var categoryRules = CategoryRulesEvaluator.Evaluate(@event.Invoice, preChecks.AmountUsd);
        var agentRecommendation = await _agent.RecommendAsync(@event.Invoice, policy, ct);

        var routeDecision = DecisionRouter.Decide(preChecks, categoryRules, agentRecommendation, policy.Thresholds);

        var citedRulesJson = JsonSerializer.Serialize(routeDecision.CitedRules);

        var decision = Decision.Create(
            @event.TrackingId,
            @event.CorrelationId,
            (int)routeDecision.Route,
            (int)routeDecision.Recommendation,
            routeDecision.Confidence,
            routeDecision.AmountUsd,
            @event.Invoice.Department,
            citedRulesJson,
            routeDecision.FraudSignal?.Detected ?? false,
            routeDecision.FraudSignal?.Reason,
            policy.Version);

        await _repo.AddAsync(decision, ct);
        await _repo.SaveChangesAsync(ct);

        var outbound = new DecisionMadeV1
        {
            TrackingId = @event.TrackingId,
            CorrelationId = @event.CorrelationId,
            OccurredAt = DateTimeOffset.UtcNow,
            Route = routeDecision.Route,
            Recommendation = routeDecision.Recommendation,
            Confidence = routeDecision.Confidence,
            CitedRules = routeDecision.CitedRules,
            FraudSignal = routeDecision.FraudSignal,
            AmountUsd = routeDecision.AmountUsd,
            Department = @event.Invoice.Department
        };

        await _publisher.PublishDecisionMadeAsync(outbound, ct);

        _logger.LogInformation(
            "Decision routed {Route} for TrackingId {TrackingId} at ${AmountUsd:F2}.",
            routeDecision.Route, @event.TrackingId, routeDecision.AmountUsd);
    }
}
