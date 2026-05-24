using MediatR;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;
using Wallet.Contracts.Responses;

namespace Wallet.Application.Wallets.Commands.ConfirmReservation;

public record ConfirmReservationCommand(
    long WalletId,
    long ReservationId,
    string IdempotencyKey,
    IRouteInfo RouteInfo) : IRequest<Result<ReservationOperationResponse>>;
