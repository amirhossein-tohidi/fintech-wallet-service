using System.Text.Json;
using Microsoft.Extensions.Options;
using Wallet.Contracts.Enums;
using Wallet.Contracts.IntegrationEvents;
using Wallet.Infrastructure.Redis;

namespace Wallet.Worker.InboxHandlers;

public sealed class InboxProjectionWriter(
    RedisConnectionFactory connectionFactory,
    IOptions<RedisOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ProjectionTtl = TimeSpan.FromMinutes(10);
    private readonly RedisOptions _options = options.Value;

    public async Task UpsertWalletBalanceAsync(
        IntegrationEventEnvelope<WalletBalanceChangedEvent> envelope,
        CancellationToken ct)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        var @event = envelope.Payload;
        var payload = JsonSerializer.Serialize(new
        {
            @event.WalletId,
            @event.UserId,
            @event.NewBalance,
            @event.AmountChanged,
            envelope.Id,
            envelope.OccurredOn
        }, JsonOptions);

        await database.StringSetAsync(
            key: BuildKey(WalletCacheKeys.Balance(@event.WalletId)),
            value: payload,
            expiry: ProjectionTtl);
    }

    public Task InvalidateWalletBalanceAsync(long walletId, CancellationToken ct)
    {
        return DeleteAsync(key: WalletCacheKeys.Balance(walletId));
    }

    public Task InvalidateWalletTransactionsAsync(long walletId, CancellationToken ct)
    {
        return DeleteAsync(key: WalletCacheKeys.Transactions(walletId));
    }

    public Task InvalidateWalletReservationsAsync(long walletId, CancellationToken ct)
    {
        return DeleteAsync(key: WalletCacheKeys.Reservations(walletId));
    }

    public Task InvalidatePromoBalancesAsync(
        long walletId,
        ContractWalletServiceType serviceType,
        CancellationToken ct)
    {
        return DeleteAsync(key: WalletCacheKeys.PromoBalances(walletId, serviceType.ToString()));
    }

    private async Task DeleteAsync(string key)
    {
        var database = await connectionFactory.GetDatabaseAsync();
        if (database == null)
        {
            return;
        }

        await database.KeyDeleteAsync(BuildKey(key));
    }

    private string BuildKey(string key)
    {
        return $"{_options.InstanceName}{key}";
    }
}
