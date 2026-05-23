using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;

namespace Wallet.Application.Wallets.Commands.ConsumePromo;

public record ConsumePromoCommand(
    long WalletId,
    DomainWalletServiceType ServiceType,
    decimal Amount,
    string IdempotencyKey,
    IRouteInfo RouteInfo) : IRequest<Result<WalletTransactionResultResponse>>;
