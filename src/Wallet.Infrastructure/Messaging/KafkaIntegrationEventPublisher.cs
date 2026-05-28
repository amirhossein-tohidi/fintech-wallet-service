using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using KafkaFlow.Producers;
using Wallet.Application.Abstractions.Messaging;
using Wallet.Contracts.Enums;
using Wallet.Infrastructure.Resilience;

namespace Wallet.Infrastructure.Messaging;

public sealed class KafkaIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaIntegrationEventPublisher> _logger;
    private readonly CircuitBreakerState _circuitBreaker;
    private readonly IProducerAccessor? _producerAccessor;

    public KafkaIntegrationEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaIntegrationEventPublisher> logger,
        CircuitBreakerState circuitBreaker,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _logger = logger;
        _circuitBreaker = circuitBreaker;

        if (_options.Enabled)
        {
            _producerAccessor = serviceProvider.GetService<IProducerAccessor>();
        }
    }

    public async Task PublishAsync(
        IntegrationEventType eventType,
        string payload,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Kafka is disabled. Skipping event {EventType}.", eventType);
            return;
        }

        if (_producerAccessor == null)
        {
            throw new InvalidOperationException("KafkaFlow producer accessor is not registered.");
        }

        var producer = _producerAccessor.GetProducer(_options.ProducerName);

        await _circuitBreaker.ExecuteAsync(
            dependencyName: "Kafka",
            operation: async () => await producer.ProduceAsync(
                _options.Topic,
                Encoding.UTF8.GetBytes(eventType.ToString()),
                Encoding.UTF8.GetBytes(payload)),
            ct: ct);
    }
}
