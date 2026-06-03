using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Events;
using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;
using Wallet.Infrastructure.Persistence.Messaging;
using Wallet.Infrastructure.Redis;
using Wallet.IntegrationTests.Infrastructure;

namespace Wallet.IntegrationTests.Worker;

public sealed class WorkerProcessingTests(WalletIntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task OutboxProcessorWorker_PublishesAndMarksApiGeneratedOutboxMessagesProcessed()
    {
        var topUpResponse = await PostAsync(
            uri: $"/api/v1/wallet/users/{Guid.NewGuid()}/topups",
            body: new Contracts.Requests.TopUpWalletRequest(100),
            idempotencyKey: $"topup-{Guid.NewGuid():N}");
        await AssertStatusAsync(topUpResponse, System.Net.HttpStatusCode.OK);

        await using (var dbContext = Fixture.CreateDbContext())
        {
            Assert.True(await dbContext.OutboxMessages.AnyAsync(x => x.ProcessedAt == null));
        }

        using var worker = await Fixture.StartWorkerAsync(outboxEnabled: true, redisEnabled: false);

        await WaitUntilAsync(async () =>
        {
            await using var dbContext = Fixture.CreateDbContext();
            return await dbContext.OutboxMessages.AnyAsync() &&
                   await dbContext.OutboxMessages.AllAsync(x => x.ProcessedAt != null);
        });
    }

    [Fact]
    public async Task ReservationExpiryWorker_ExpiresExpiredReservationsAndRestoresReservedBalance()
    {
        long walletId;
        long reservationId;

        await using (var dbContext = Fixture.CreateDbContext())
        {
            var wallet = new UserWallet(Guid.NewGuid());
            dbContext.UserWallets.Add(wallet);

            wallet.TopUp(amount: 500, idem: $"seed-topup-{Guid.NewGuid():N}");
            await dbContext.SaveChangesAsync();

            var reservation = wallet.CreateReservation(
                serviceType: DomainWalletServiceType.Travel,
                amount: 200,
                expireAt: DateTime.UtcNow.AddSeconds(-5),
                idem: $"seed-reserve-{Guid.NewGuid():N}");
            await dbContext.SaveChangesAsync();

            walletId = wallet.Id;
            reservationId = reservation.Id;
        }

        using var worker = await Fixture.StartWorkerAsync(reservationExpiryEnabled: true, redisEnabled: false);

        await WaitUntilAsync(async () =>
        {
            await using var dbContext = Fixture.CreateDbContext();
            var wallet = await dbContext.UserWallets.SingleAsync(x => x.Id == walletId);
            var reservation = await dbContext.Reservations.SingleAsync(x => x.Id == reservationId);

            return reservation.Status == ReservationStatus.Expired &&
                   wallet.AvailableBalance == 500 &&
                   wallet.ReservedBalance == 0;
        });
    }

    [Fact]
    public async Task InboxProcessorWorker_ProcessesInboxMessageAndWritesRedisProjection()
    {
        const long walletId = 4242;
        var envelope = new IntegrationEventEnvelope<WalletBalanceChangedEvent>(
            Id: Guid.NewGuid(),
            Type: IntegrationEventType.WalletBalanceChanged,
            OccurredOn: DateTime.UtcNow,
            Payload: new WalletBalanceChangedEvent(
                UserId: Guid.NewGuid(),
                WalletId: walletId,
                NewBalance: 750,
                AmountChanged: 750));

        await using (var dbContext = Fixture.CreateDbContext())
        {
            dbContext.InboxMessages.Add(new InboxMessage(
                id: envelope.Id,
                eventType: envelope.Type,
                payload: JsonSerializer.Serialize(envelope, JsonOptions),
                occurredOn: envelope.OccurredOn));
            await dbContext.SaveChangesAsync();
        }

        using var worker = await Fixture.StartWorkerAsync(inboxEnabled: true, redisEnabled: true);

        await WaitUntilAsync(async () =>
        {
            await using var dbContext = Fixture.CreateDbContext();
            var messageProcessed = await dbContext.InboxMessages
                .AnyAsync(x => x.Id == envelope.Id && x.ProcessedAt != null);

            var projection = await Fixture.GetRedisStringAsync($"wallet-it:{WalletCacheKeys.Balance(walletId)}");

            return messageProcessed &&
                   projection is not null &&
                   projection.Contains("\"newBalance\":750", StringComparison.Ordinal);
        });
    }
}
