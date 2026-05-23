using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;

namespace Wallet.Infrastructure.Dapper;

internal static class TransactionReadRowMappingExtensions
{
    public static TransactionResponse ToResponse(this TransactionReadRow row)
    {
        return new TransactionResponse(
            TransactionId: row.TransactionId,
            WalletId: row.WalletId,
            TransactionType: row.Type.ToContract(),
            ServiceType: row.ServiceType.ToContract(),
            Amount: row.Amount,
            ReferenceId: row.ReferenceId,
            CreatedAt: row.CreatedAt);
    }
}
