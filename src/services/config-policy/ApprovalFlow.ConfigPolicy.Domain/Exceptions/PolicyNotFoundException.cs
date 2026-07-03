namespace ApprovalFlow.ConfigPolicy.Domain.Exceptions;

public sealed class PolicyNotFoundException : Exception
{
    public PolicyNotFoundException(Guid id)
        : base($"Policy document '{id}' was not found.") { }
}
