namespace Wallet.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "wallet.domain-events";
    public string ClientId { get; set; } = "wallet-service";
    public string ClusterName { get; set; } = "wallet-kafka";
    public string ProducerName { get; set; } = "wallet-events-producer";
    public string ConsumerName { get; set; } = "wallet-events-inbox-consumer";
    public string ConsumerGroupId { get; set; } = "wallet-inbox";
    public int ConsumerWorkers { get; set; } = 4;
    public int ConsumerBufferSize { get; set; } = 100;
    public int ConsumerMinWorkers { get; set; } = 1;
    public int ConsumerMaxWorkers { get; set; } = 8;
    public int AutoCommitIntervalMs { get; set; } = 5000;
    public int StatisticsIntervalMs { get; set; } = 30000;
    public int ReconnectBackoffMs { get; set; } = 1000;
    public int ReconnectBackoffMaxMs { get; set; } = 30000;
    public int RetryBackoffMs { get; set; } = 1000;
    public int MessageTimeoutMs { get; set; } = 120000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public int SessionTimeoutMs { get; set; } = 10000;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public bool CreateTopicIfNotExists { get; set; }
    public int TopicPartitions { get; set; } = 3;
    public short TopicReplicationFactor { get; set; } = 1;
}
