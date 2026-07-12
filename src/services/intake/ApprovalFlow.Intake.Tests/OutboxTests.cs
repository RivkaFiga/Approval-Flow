using ApprovalFlow.Contracts.Events;
using ApprovalFlow.Contracts.Events.V1;
using ApprovalFlow.Contracts.Models;
using ApprovalFlow.Intake.Application.Ports;
using ApprovalFlow.Intake.Domain.Entities;
using ApprovalFlow.Intake.Infrastructure.Events;
using ApprovalFlow.Intake.Infrastructure.Outbox;
using ApprovalFlow.Intake.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ApprovalFlow.Intake.Tests;

public class OutboxTests
{
    private static DbContextOptions<IntakeDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<IntakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task OutboxPublisher_stages_invoice_submitted_event_without_saving()
    {
        var options = NewInMemoryOptions();
        using var db = new IntakeDbContext(options);
        var publisher = new OutboxIntakeEventPublisher(db);

        var @event = new InvoiceSubmittedV1
        {
            TrackingId = "TRK-x",
            Invoice = new Invoice { InvoiceNumber = "INV-1", Vendor = "Acme" },
            CorrelationId = "corr-1",
            OccurredAt = DateTimeOffset.UtcNow
        };

        await publisher.PublishInvoiceSubmittedAsync(@event);

        // Not saved yet — only tracked.
        Assert.Equal(0, await db.OutboxMessages.CountAsync());

        await db.SaveChangesAsync();

        var stored = await db.OutboxMessages.SingleAsync();
        Assert.Equal(EventTypes.InvoiceSubmitted, stored.EventType);
        Assert.Equal("corr-1", stored.CorrelationId);
        Assert.Null(stored.DispatchedAt);
        Assert.Equal(0, stored.AttemptCount);
    }

    [Fact]
    public async Task OutboxDispatcher_publishes_pending_messages_and_marks_dispatched()
    {
        var options = NewInMemoryOptions();
        using (var seed = new IntakeDbContext(options))
        {
            seed.OutboxMessages.Add(OutboxMessage.Create(
                EventTypes.InvoiceSubmitted, "{\"trackingId\":\"TRK-1\"}", DateTimeOffset.UtcNow, "corr-1"));
            seed.OutboxMessages.Add(OutboxMessage.Create(
                EventTypes.InvoiceSubmitted, "{\"trackingId\":\"TRK-2\"}", DateTimeOffset.UtcNow, "corr-2"));
            await seed.SaveChangesAsync();
        }

        var bus = Substitute.For<IEventBusPublisher>();
        var dispatcher = BuildDispatcher(options, bus);

        await RunUntilAsync(dispatcher, async () =>
        {
            using var check = new IntakeDbContext(options);
            return await check.OutboxMessages.CountAsync(m => m.DispatchedAt == null) == 0;
        });

        await bus.Received(2).PublishAsync(
            EventTypes.InvoiceSubmitted,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        using var verify = new IntakeDbContext(options);
        Assert.Equal(0, await verify.OutboxMessages.CountAsync(m => m.DispatchedAt == null));
    }

    [Fact]
    public async Task OutboxDispatcher_records_failure_and_leaves_message_pending()
    {
        var options = NewInMemoryOptions();
        using (var seed = new IntakeDbContext(options))
        {
            seed.OutboxMessages.Add(OutboxMessage.Create(
                EventTypes.InvoiceSubmitted, "{\"trackingId\":\"TRK-X\"}", DateTimeOffset.UtcNow, "corr-x"));
            await seed.SaveChangesAsync();
        }

        var bus = Substitute.For<IEventBusPublisher>();
        bus.PublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("broker down"));

        var dispatcher = BuildDispatcher(options, bus);

        await RunUntilAsync(dispatcher, async () =>
        {
            using var check = new IntakeDbContext(options);
            var m = await check.OutboxMessages.SingleAsync();
            return m.AttemptCount >= 1;
        });

        using var verify = new IntakeDbContext(options);
        var stored = await verify.OutboxMessages.SingleAsync();
        Assert.Null(stored.DispatchedAt);
        Assert.True(stored.AttemptCount >= 1);
        Assert.Contains("broker down", stored.LastError);
    }

    private static OutboxDispatcher BuildDispatcher(
        DbContextOptions<IntakeDbContext> options,
        IEventBusPublisher bus)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new IntakeDbContext(options));
        services.AddScoped<IOutboxDispatchStore, OutboxDispatchStore>();
        services.AddScoped(_ => bus);
        var provider = services.BuildServiceProvider();

        var opts = Options.Create(new OutboxOptions
        {
            PollingIntervalMs = 50,
            BatchSize = 50,
            MaxAttempts = 5
        });
        return new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            opts,
            NullLogger<OutboxDispatcher>.Instance);
    }

    private static async Task RunUntilAsync(
        OutboxDispatcher dispatcher,
        Func<Task<bool>> predicate,
        int timeoutMs = 5000)
    {
        var cts = new CancellationTokenSource();
        var task = dispatcher.StartAsync(cts.Token);
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                if (await predicate())
                    return;
                await Task.Delay(50);
            }
        }
        finally
        {
            cts.Cancel();
            try { await dispatcher.StopAsync(CancellationToken.None); } catch { }
            try { await task; } catch { }
        }
    }
}
