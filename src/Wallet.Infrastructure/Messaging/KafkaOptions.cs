namespace Wallet.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "wallet.domain-events";
    public string ClientId { get; set; } = "wallet-service";
}
