using Wallet.Contracts.Enums;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class LedgerTransactionTypeGrpcMapping
{
    public static LedgerTransactionType ToGrpc(this ContractLedgerTransactionType transactionType)
    {
        return transactionType switch
        {
            ContractLedgerTransactionType.TopUp => LedgerTransactionType.TopUp,
            ContractLedgerTransactionType.Payment => LedgerTransactionType.Payment,
            ContractLedgerTransactionType.Hold => LedgerTransactionType.Hold,
            ContractLedgerTransactionType.Capture => LedgerTransactionType.Capture,
            ContractLedgerTransactionType.Release => LedgerTransactionType.Release,
            ContractLedgerTransactionType.Refund => LedgerTransactionType.Refund,
            ContractLedgerTransactionType.PromoConsume => LedgerTransactionType.PromoConsume,
            _ => throw new NotImplementedException($"gRPC mapping is not implemented for contract ledger transaction type '{transactionType}'.")
        };
    }
}
