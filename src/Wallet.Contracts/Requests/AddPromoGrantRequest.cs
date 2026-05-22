namespace Wallet.Contracts.Requests;

public record AddPromoGrantRequest(
    decimal Amount,
    DateTime ExpiresAt);
