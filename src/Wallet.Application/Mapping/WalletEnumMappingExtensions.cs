using Wallet.Contracts.Enums;
using Wallet.Domain.Enums;

namespace Wallet.Application.Mapping;

public static class WalletEnumMappingExtensions
{
    public static ContractWalletServiceType ToContract(this DomainWalletServiceType serviceType)
    {
        return serviceType switch
        {
            DomainWalletServiceType.General => ContractWalletServiceType.General,
            DomainWalletServiceType.Travel => ContractWalletServiceType.Travel,
            DomainWalletServiceType.Food => ContractWalletServiceType.Food,
            DomainWalletServiceType.Shop => ContractWalletServiceType.Shop,
            _ => ContractWalletServiceType.General
        };
    }

    public static DomainWalletServiceType ToDomain(this ContractWalletServiceType serviceType)
    {
        return serviceType switch
        {
            ContractWalletServiceType.General => DomainWalletServiceType.General,
            ContractWalletServiceType.Travel => DomainWalletServiceType.Travel,
            ContractWalletServiceType.Food => DomainWalletServiceType.Food,
            ContractWalletServiceType.Shop => DomainWalletServiceType.Shop,
            _ => DomainWalletServiceType.General
        };
    }

    public static ContractLedgerTransactionType ToContract(this LedgerTransactionType transactionType)
    {
        return transactionType switch
        {
            LedgerTransactionType.TopUp => ContractLedgerTransactionType.TopUp,
            LedgerTransactionType.Payment => ContractLedgerTransactionType.Payment,
            LedgerTransactionType.Hold => ContractLedgerTransactionType.Hold,
            LedgerTransactionType.Capture => ContractLedgerTransactionType.Capture,
            LedgerTransactionType.Release => ContractLedgerTransactionType.Release,
            LedgerTransactionType.Refund => ContractLedgerTransactionType.Refund,
            LedgerTransactionType.PromoConsume => ContractLedgerTransactionType.PromoConsume,
            _ => throw new ArgumentOutOfRangeException(nameof(transactionType), transactionType, "Unsupported ledger transaction type.")
        };
    }
}
