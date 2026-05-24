namespace Wallet.Worker.BackgroundJobs;

public sealed class PromoExpiryWorker(ILogger<PromoExpiryWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Promo grants expire by computed state. No background mutation is required.");
        return Task.CompletedTask;
    }
}
