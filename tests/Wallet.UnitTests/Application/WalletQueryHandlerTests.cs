using Wallet.Application.Abstractions.ReadModels;
using Wallet.Application.Queries.GetPromoBalances;
using Wallet.Application.Queries.GetTransactions;
using Wallet.Application.Queries.GetWalletBalance;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;

namespace Wallet.UnitTests.Application;

public sealed class WalletQueryHandlerTests
{
    [Fact]
    public async Task GivenBalanceQuery_WhenHandled_ThenReadRepositoryIsCalled()
    {
        var repository = new FakeWalletReadRepository
        {
            Balance = new WalletBalanceResponse
            {
                WalletId = 10,
                UserId = Guid.NewGuid(),
                AvailableBalance = 100,
                ReservedBalance = 20
            }
        };
        var handler = new GetWalletBalanceHandler(repository);

        var result = await handler.Handle(new GetWalletBalanceQuery(WalletId: 10), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(10, repository.LastBalanceWalletId);
        Assert.Equal(120, result.TotalRealBalance);
    }

    [Fact]
    public async Task GivenPromoBalanceQuery_WhenHandled_ThenReadRepositoryIsCalledWithServiceType()
    {
        var repository = new FakeWalletReadRepository
        {
            PromoBalances =
            [
                new PromoBalanceResponse
                {
                    PromoGrantId = 1,
                    ServiceType = ContractWalletServiceType.Travel,
                    OriginalAmount = 100,
                    RemainingAmount = 40,
                    ExpiresAt = DateTime.UtcNow.AddDays(1)
                }
            ]
        };
        var handler = new GetPromoBalancesHandler(repository);

        var result = await handler.Handle(
            new GetPromoBalancesQuery(WalletId: 10, ServiceType: ContractWalletServiceType.Travel),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, repository.LastPromoWalletId);
        Assert.Equal(ContractWalletServiceType.Travel, repository.LastPromoServiceType);
    }

    [Fact]
    public async Task GivenTransactionsQuery_WhenHandled_ThenReadRepositoryIsCalledWithServiceType()
    {
        var repository = new FakeWalletReadRepository
        {
            Transactions =
            [
                new TransactionResponse(
                    TransactionId: 1,
                    WalletId: 10,
                    TransactionType: ContractLedgerTransactionType.Payment,
                    ServiceType: ContractWalletServiceType.Food,
                    Amount: 50,
                    ReferenceId: null,
                    CreatedAt: DateTime.UtcNow)
            ]
        };
        var handler = new GetTransactionsHandler(repository);

        var result = await handler.Handle(
            new GetTransactionsQuery(WalletId: 10, ServiceType: ContractWalletServiceType.Food),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(10, repository.LastTransactionsWalletId);
        Assert.Equal(ContractWalletServiceType.Food, repository.LastTransactionsServiceType);
    }

    private sealed class FakeWalletReadRepository : IWalletReadRepository
    {
        public WalletBalanceResponse? Balance { get; init; }
        public IReadOnlyCollection<PromoBalanceResponse> PromoBalances { get; init; } = [];
        public IReadOnlyCollection<TransactionResponse> Transactions { get; init; } = [];
        public long? LastBalanceWalletId { get; private set; }
        public long? LastPromoWalletId { get; private set; }
        public ContractWalletServiceType? LastPromoServiceType { get; private set; }
        public long? LastTransactionsWalletId { get; private set; }
        public ContractWalletServiceType? LastTransactionsServiceType { get; private set; }

        public Task<WalletBalanceResponse?> GetBalanceAsync(long walletId, CancellationToken ct)
        {
            LastBalanceWalletId = walletId;
            return Task.FromResult(Balance);
        }

        public Task<IReadOnlyCollection<PromoBalanceResponse>> GetPromoBalancesAsync(
            long walletId,
            ContractWalletServiceType? serviceType,
            CancellationToken ct)
        {
            LastPromoWalletId = walletId;
            LastPromoServiceType = serviceType;
            return Task.FromResult(PromoBalances);
        }

        public Task<IReadOnlyCollection<TransactionResponse>> GetTransactionsAsync(
            long walletId,
            ContractWalletServiceType? serviceType,
            CancellationToken ct)
        {
            LastTransactionsWalletId = walletId;
            LastTransactionsServiceType = serviceType;
            return Task.FromResult(Transactions);
        }
    }
}
