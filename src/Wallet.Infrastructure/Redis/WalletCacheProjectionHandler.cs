using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Wallet.Domain.Events.Reservation;
using Wallet.Domain.Events.Wallet;

namespace Wallet.Infrastructure.Redis;

public sealed class WalletCacheProjectionHandler(
    RedisConnectionFactory connectionFactory,
    IOptions<RedisOptions> options)
    : INotificationHandler<WalletBalanceChanged>,
      INotificationHandler<ReservationConfirmed>,
      INotificationHandler<ReservationCancelled>,
      INotificationHandler<ReservationExpired>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisOptions _options = options.Value;

    public async Task Handle(WalletBalanceChanged notification, CancellationToken ct)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            notification.WalletId,
            notification.UserId,
            notification.NewBalance,
            notification.AmountChanged,
            notification.OccurredOn
        }, JsonOptions);

        await database.StringSetAsync(
            BuildKey(WalletCacheKeys.Balance(notification.WalletId)),
            payload,
            TimeSpan.FromMinutes(10));
    }

    public Task Handle(ReservationConfirmed notification, CancellationToken ct)
    {
        return RemoveBalanceAsync(notification.WalletId);
    }

    public Task Handle(ReservationCancelled notification, CancellationToken ct)
    {
        return RemoveBalanceAsync(notification.WalletId);
    }

    public Task Handle(ReservationExpired notification, CancellationToken ct)
    {
        return RemoveBalanceAsync(notification.WalletId);
    }

    private async Task RemoveBalanceAsync(long walletId)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        await database.KeyDeleteAsync(BuildKey(WalletCacheKeys.Balance(walletId)));
    }

    private string BuildKey(string key)
    {
        return $"{_options.InstanceName}{key}";
    }
}
