namespace Wallet.Worker.BackgroundJobs;

public sealed class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int LockSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 10;
    public int DeadLetterBatchSize { get; set; } = 10;
    public int DeadLetterPollingIntervalSeconds { get; set; } = 60;
}
