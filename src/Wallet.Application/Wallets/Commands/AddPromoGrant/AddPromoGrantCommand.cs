using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;

namespace Wallet.Application.Wallets.Commands.AddPromoGrant;

public record AddPromoGrantCommand(
    long WalletId,
    DomainWalletServiceType ServiceType,
    decimal Amount,
    DateTime ExpiresAt,
    string IdempotencyKey,
    IRouteInfo RouteInfo) : IRequest<Result<PromoGrantOperationResponse>>;
