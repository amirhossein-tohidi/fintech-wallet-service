using Wallet.Application.Abstractions.ReadModels;
using Wallet.Application.Mapping;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Dapper;

public class WalletReadRepository(WalletDbContext dbContext)
    : BaseReadRepository(dbContext), IWalletReadRepository
{
    public async Task<WalletBalanceResponse?> GetBalanceAsync(long walletId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                Id AS WalletId,
                UserId,
                AvailableBalance,
                ReservedBalance
            FROM UserWallets
            WHERE Id = @WalletId
            """;

        return await QuerySingleOrDefaultAsync<WalletBalanceResponse>(
            sql: sql,
            parameters: new { WalletId = walletId },
            ct: ct);
    }

    public async Task<IReadOnlyCollection<PromoBalanceResponse>> GetPromoBalancesAsync(
        long walletId,
        ContractWalletServiceType? serviceType,
        CancellationToken ct)
    {
        var domainServiceType = serviceType.HasValue
            ? serviceType.Value.ToDomain()
            : (DomainWalletServiceType?)null;

        const string sql = """
            SELECT
                Id AS PromoGrantId,
                ServiceType,
                Amount AS OriginalAmount,
                RemainingAmount,
                ExpiresAt
            FROM PromoGrants
            WHERE WalletId = @WalletId
              AND (@ServiceType IS NULL OR ServiceType = @ServiceType)
            ORDER BY ExpiresAt ASC
            """;

        return await QueryAsync<PromoBalanceResponse>(
            sql: sql,
            parameters: new { WalletId = walletId, ServiceType = (int?)domainServiceType },
            ct: ct);
    }

    public async Task<IReadOnlyCollection<TransactionResponse>> GetTransactionsAsync(
        long walletId,
        ContractWalletServiceType? serviceType,
        CancellationToken ct)
    {
        var domainServiceType = serviceType.HasValue
            ? serviceType.Value.ToDomain()
            : (DomainWalletServiceType?)null;

        const string sql = """
            SELECT
                Id AS TransactionId,
                WalletId,
                Type,
                ServiceType,
                Amount,
                ReferenceId,
                CreatedAt
            FROM LedgerTransactions
            WHERE WalletId = @WalletId
              AND (@ServiceType IS NULL OR ServiceType = @ServiceType)
            ORDER BY CreatedAt DESC
            """;

        var rows = await QueryAsync<TransactionReadRow>(
            sql: sql,
            parameters: new { WalletId = walletId, ServiceType = (int?)domainServiceType },
            ct: ct);

        return rows
            .Select(row => row.ToResponse())
            .ToList();
    }
}
