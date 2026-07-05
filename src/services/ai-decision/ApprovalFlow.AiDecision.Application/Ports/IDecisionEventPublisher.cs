using ApprovalFlow.Contracts.Events.V1;

namespace ApprovalFlow.AiDecision.Application.Ports;

public interface IDecisionEventPublisher
{
    Task PublishDecisionMadeAsync(DecisionMadeV1 @event, CancellationToken ct = default);
}
