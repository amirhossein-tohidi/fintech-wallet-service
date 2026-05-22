using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;
using Wallet.Domain.Enums;

namespace Wallet.Application.Wallets.Commands.FastPay;

public record FastPayCommand(
    long WalletId,
    DomainWalletServiceType ServiceType,
    decimal Amount,
    string IdempotencyKey,
    IRouteInfo RouteInfo) : IRequest<Result<WalletTransactionResultResponse>>;
