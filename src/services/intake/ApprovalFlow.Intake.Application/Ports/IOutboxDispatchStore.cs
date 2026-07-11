using ApprovalFlow.Intake.Domain.Entities;

namespace ApprovalFlow.Intake.Application.Ports;

public interface IOutboxDispatchStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int maxCount, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
