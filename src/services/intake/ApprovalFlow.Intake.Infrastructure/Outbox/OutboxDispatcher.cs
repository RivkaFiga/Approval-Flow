using ApprovalFlow.Intake.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Intake.Infrastructure.Outbox;

/// <summary>
/// Polls the Outbox for undispatched messages, publishes each through the event bus
/// (Dapr pubsub), then marks it dispatched. A message is marked dispatched only after
/// a successful publish, so a crash between publish and mark results in at-least-once
/// delivery — downstream consumers already deduplicate by TrackingId, so this is safe.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly OutboxOptions _options;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxDispatcher started (interval {IntervalMs}ms, batch {BatchSize}).",
            _options.PollingIntervalMs,
            _options.BatchSize);

        var pollDelay = TimeSpan.FromMilliseconds(Math.Max(100, _options.PollingIntervalMs));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch iteration failed.");
            }

            try
            {
                await Task.Delay(pollDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("OutboxDispatcher stopping.");
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxDispatchStore>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBusPublisher>();

        var pending = await store.GetPendingAsync(_options.BatchSize, ct);
        if (pending.Count == 0)
            return;

        _logger.LogDebug("Dispatching {Count} outbox messages.", pending.Count);

        var anyChanges = false;

        foreach (var message in pending)
        {
            if (ct.IsCancellationRequested)
                break;

            if (message.AttemptCount >= _options.MaxAttempts)
            {
                _logger.LogWarning(
                    "Skipping outbox message {MessageId} ({EventType}) — reached MaxAttempts {MaxAttempts}.",
                    message.Id, message.EventType, _options.MaxAttempts);
                continue;
            }

            try
            {
                await bus.PublishAsync(message.EventType, message.Payload, ct);
                message.MarkDispatched(DateTimeOffset.UtcNow);
                anyChanges = true;

                _logger.LogInformation(
                    "Dispatched outbox message {MessageId} ({EventType}) correlation={CorrelationId}.",
                    message.Id, message.EventType, message.CorrelationId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message);
                anyChanges = true;

                _logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} ({EventType}); attempt {Attempt}.",
                    message.Id, message.EventType, message.AttemptCount);
            }
        }

        if (anyChanges)
            await store.SaveChangesAsync(ct);
    }
}
