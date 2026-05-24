using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wallet.Domain.Enums;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Worker.BackgroundJobs;

public sealed class ReservationExpiryOptions
{
    public const string SectionName = "ReservationExpiry";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 30;
}

public sealed class ReservationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationExpiryOptions> options,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    private readonly ReservationExpiryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Reservation expiry worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reservation expiry worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var reservationIds = await dbContext.Reservations
            .Where(x => x.Status == ReservationStatus.Created && x.ExpireAt <= DateTime.UtcNow)
            .OrderBy(x => x.ExpireAt)
            .Take(_options.BatchSize)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var reservationId in reservationIds)
        {
            await ExpireReservationAsync(reservationId: reservationId, ct: ct);
        }
    }

    private async Task ExpireReservationAsync(long reservationId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var reservation = await dbContext.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == reservationId, ct);

        if (reservation == null || reservation.Status != ReservationStatus.Created)
        {
            return;
        }

        var wallet = await dbContext.UserWallets
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == reservation.WalletId, ct);

        if (wallet == null)
        {
            return;
        }

        try
        {
            wallet.ExpireReservation(reservationId: reservationId);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogInformation(ex, "Reservation {ReservationId} was processed by another instance.", reservationId);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(ex, "Reservation {ReservationId} is no longer eligible for expiry.", reservationId);
        }
    }
}
