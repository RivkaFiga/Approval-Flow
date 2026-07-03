namespace ApprovalFlow.ConfigPolicy.Application.Ports;

public interface IPolicyEventPublisher
{
    Task PublishPolicyChangedAsync(Guid policyId, int version, CancellationToken ct = default);
}
