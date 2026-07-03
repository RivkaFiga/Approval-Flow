using ApprovalFlow.ConfigPolicy.Domain.Entities;

namespace ApprovalFlow.ConfigPolicy.Application.Ports;

public interface IPolicyRepository
{
    Task<PolicyDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PolicyDocument?> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PolicyDocument>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PolicyDocument document, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
