namespace Wallet.Worker.BackgroundJobs;

public sealed class InboxProcessorOptions
{
    public const string SectionName = "InboxProcessor";

    public bool Enabled { get; set; }
    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int LockSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 10;
    public int DeadLetterBatchSize { get; set; } = 10;
    public int DeadLetterPollingIntervalSeconds { get; set; } = 120;
}
