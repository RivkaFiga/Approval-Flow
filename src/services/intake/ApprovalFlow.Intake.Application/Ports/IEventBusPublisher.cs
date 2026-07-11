namespace ApprovalFlow.Intake.Application.Ports;

public interface IEventBusPublisher
{
    Task PublishAsync(string eventType, string payloadJson, CancellationToken ct = default);
}
