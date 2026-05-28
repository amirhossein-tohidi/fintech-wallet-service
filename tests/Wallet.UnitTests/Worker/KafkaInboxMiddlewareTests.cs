using System.Text.Json;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;
using Wallet.Infrastructure.Persistence;
using Wallet.Worker.Messaging;

namespace Wallet.UnitTests.Worker;

public sealed class KafkaInboxMiddlewareTests
{
    [Fact]
    public async Task Invoke_WhenMessageIsNew_PersistsInboxMessageAndCompletesOffset()
    {
        await using var dbContext = CreateDbContext();
        var envelope = CreateEnvelope();
        var middleware = CreateMiddleware(dbContext);
        var context = new FakeMessageContext(JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        await middleware.Invoke(context, _ => Task.CompletedTask);

        var message = await dbContext.InboxMessages.SingleAsync();
        Assert.Equal(envelope.Id, message.Id);
        Assert.Equal(IntegrationEventType.WalletBalanceChanged, message.EventType);
        Assert.Equal(envelope.OccurredOn, message.OccurredOn);
        Assert.True(((FakeConsumerContext)context.ConsumerContext).Completed);
    }

    [Fact]
    public async Task Invoke_WhenMessageAlreadyExists_DoesNotInsertDuplicateAndCompletesOffset()
    {
        await using var dbContext = CreateDbContext();
        var envelope = CreateEnvelope();
        var payload = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        dbContext.InboxMessages.Add(new(
            id: envelope.Id,
            eventType: envelope.Type,
            payload: payload,
            occurredOn: envelope.OccurredOn));
        await dbContext.SaveChangesAsync();

        var middleware = CreateMiddleware(dbContext);
        var context = new FakeMessageContext(payload);

        await middleware.Invoke(context, _ => Task.CompletedTask);

        Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
        Assert.True(((FakeConsumerContext)context.ConsumerContext).Completed);
    }

    private static IntegrationEventEnvelope<WalletBalanceChangedEvent> CreateEnvelope()
    {
        return new(
            Id: Guid.NewGuid(),
            Type: IntegrationEventType.WalletBalanceChanged,
            OccurredOn: DateTime.UtcNow,
            Payload: new WalletBalanceChangedEvent(
                UserId: Guid.NewGuid(),
                WalletId: 42,
                NewBalance: 100,
                AmountChanged: 100));
    }

    private static WalletDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase($"kafka-inbox-{Guid.NewGuid():N}")
            .Options;

        return new(options);
    }

    private static KafkaInboxMiddleware CreateMiddleware(WalletDbContext dbContext)
    {
        var services = new ServiceCollection()
            .AddSingleton(dbContext)
            .BuildServiceProvider();

        return new(
            scopeFactory: services.GetRequiredService<IServiceScopeFactory>(),
            logger: NullLogger<KafkaInboxMiddleware>.Instance);
    }

    private sealed class FakeMessageContext(string payload) : IMessageContext
    {
        public Message Message { get; } = new(
            key: IntegrationEventType.WalletBalanceChanged.ToString(),
            value: payload);

        public IMessageHeaders Headers { get; } = null!;
        public IConsumerContext ConsumerContext { get; } = new FakeConsumerContext();
        public IProducerContext ProducerContext { get; } = null!;
        public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
        public IDependencyResolver DependencyResolver { get; } = null!;
        public IReadOnlyCollection<string> Brokers { get; } = [];

        public IMessageContext SetMessage(object key, object value)
        {
            return new FakeMessageContext(value.ToString() ?? string.Empty);
        }
    }

    private sealed class FakeConsumerContext : IConsumerContext
    {
        private readonly TaskCompletionSource<TopicPartitionOffset> _completion = new();

        public bool Completed { get; private set; }
        public string ConsumerName => "test-consumer";
        public CancellationToken WorkerStopped => CancellationToken.None;
        public int WorkerId => 1;
        public string Topic => "wallet.domain-events";
        public int Partition => 0;
        public long Offset => 1;
        public TopicPartitionOffset TopicPartitionOffset => new(Topic, Partition, Offset);
        public string GroupId => "wallet-inbox-test";
        public DateTime MessageTimestamp => DateTime.UtcNow;
        public bool AutoMessageCompletion { get; set; }
        public bool ShouldStoreOffset { get; set; } = true;
        public IDependencyResolver ConsumerDependencyResolver => null!;
        public IDependencyResolver WorkerDependencyResolver => null!;
        public Task<TopicPartitionOffset> Completion => _completion.Task;

        public void Complete()
        {
            Completed = true;
            _completion.TrySetResult(TopicPartitionOffset);
        }

        public IOffsetsWatermark GetOffsetsWatermark()
        {
            throw new NotSupportedException();
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Pause(IReadOnlyList<TopicPartition> topicPartitions)
        {
        }

        public void Resume(IReadOnlyList<TopicPartition> topicPartitions)
        {
        }
    }
}
