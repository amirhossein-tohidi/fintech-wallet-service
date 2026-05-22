using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Wallets.Commands.TopUpWallet;

public record TopUpWalletCommand(
    Guid UserId, 
    decimal Amount, 
    string IdempotencyKey,
    IRouteInfo RouteInfo) : IRequest<Result<WalletTransactionResultResponse>>;
