using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Contracts.Enums;
using Wallet.Infrastructure.Resilience;

namespace Wallet.Infrastructure.Messaging;

public sealed class KafkaIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaIntegrationEventPublisher> _logger;
    private readonly CircuitBreakerState _circuitBreaker;
    private readonly IProducer<string, string>? _producer;

    public KafkaIntegrationEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaIntegrationEventPublisher> logger,
        CircuitBreakerState circuitBreaker)
    {
        _options = options.Value;
        _logger = logger;
        _circuitBreaker = circuitBreaker;

        if (!_options.Enabled)
        {
            return;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = _options.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(
        IntegrationEventType eventType,
        string payload,
        CancellationToken ct = default)
    {
        if (!_options.Enabled || _producer == null)
        {
            _logger.LogDebug("Kafka is disabled. Skipping event {EventType}.", eventType);
            return;
        }

        var message = new Message<string, string>
        {
            Key = eventType.ToString(),
            Value = payload
        };

        await _circuitBreaker.ExecuteAsync(
            dependencyName: "Kafka",
            operation: async () => await _producer.ProduceAsync(_options.Topic, message, ct),
            ct: ct);
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
