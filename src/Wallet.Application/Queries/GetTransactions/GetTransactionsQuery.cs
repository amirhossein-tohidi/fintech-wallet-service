using MediatR;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetTransactions;

public record GetTransactionsQuery(
    long WalletId,
    ContractWalletServiceType? ServiceType = null) : IRequest<IReadOnlyCollection<TransactionResponse>>;
