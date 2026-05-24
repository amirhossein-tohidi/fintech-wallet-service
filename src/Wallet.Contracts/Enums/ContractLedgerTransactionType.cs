namespace Wallet.Contracts.Enums;

public enum ContractLedgerTransactionType
{
    TopUp = 0,
    Payment = 1,
    Hold = 2,
    Capture = 3,
    Release = 4,
    Refund = 5,
    PromoConsume = 6
}
