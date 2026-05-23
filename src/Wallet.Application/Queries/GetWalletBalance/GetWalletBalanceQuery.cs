using MediatR;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(long WalletId) : IRequest<WalletBalanceResponse?>;
