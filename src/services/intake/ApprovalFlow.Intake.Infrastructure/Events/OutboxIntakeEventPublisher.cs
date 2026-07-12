using System.Text.Json;
using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Domain.Entities;
using ApprovalFlow.Intake.Infrastructure.Persistence;

namespace ApprovalFlow.Intake.Infrastructure.Events;

/// <summary>
/// Stages an <c>invoice.submitted</c> event as an <see cref="OutboxMessage"/> row in the same
/// <see cref="IntakeDbContext"/> that persists the invoice, so both are committed atomically by
/// <see cref="IntakeDbContext.SaveChangesAsync(CancellationToken)"/>. The <c>OutboxDispatcher</c>
/// background service publishes staged messages through Dapr.
/// </summary>
public sealed class OutboxIntakeEventPublisher : IIntakeEventPublisher
{
    // Matches the DaprClient default (JsonSerializerDefaults.Web = camelCase) so the wire
    // format emitted by the dispatcher is byte-identical to the pre-outbox direct publish.
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IntakeDbContext _db;

    public OutboxIntakeEventPublisher(IntakeDbContext db) => _db = db;

    public async Task PublishInvoiceSubmittedAsync(InvoiceSubmittedV1 @event, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(@event, SerializerOptions);
        var message = OutboxMessage.Create(
            EventTypes.InvoiceSubmitted,
            payload,
            @event.OccurredAt,
            @event.CorrelationId);

        await _db.OutboxMessages.AddAsync(message, ct);
    }
}
