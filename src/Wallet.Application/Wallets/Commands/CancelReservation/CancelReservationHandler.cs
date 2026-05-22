using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Common;
using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;

namespace Wallet.Application.Wallets.Commands.CancelReservation;

public class CancelReservationHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<CancelReservationCommand, Result<ReservationOperationResponse>>
{
    public async Task<Result<ReservationOperationResponse>> Handle(CancelReservationCommand request, CancellationToken ct)
    {
        var reservation = await context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ReservationId && x.WalletId == request.WalletId, ct);

        if (reservation == null)
        {
            return Result<ReservationOperationResponse>.Failure("Reservation not found.");
        }

        var wallet = await context.UserWallets
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.Id == request.WalletId, ct);

        if (wallet == null)
        {
            return Result<ReservationOperationResponse>.Failure("Wallet not found.");
        }

        LedgerTransaction transaction;
        try
        {
            transaction = wallet.CancelReservation(request.ReservationId, request.IdempotencyKey);
            await context.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ReservationOperationResponse>.Failure(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ReservationOperationResponse>.Failure(WalletCommandErrors.ConcurrencyConflict);
        }

        var cancelledReservation = wallet.Reservations.First(x => x.Id == request.ReservationId);
        var response = mapper.Map<ReservationOperationResponse>(
            new ReservationOperationMappingSource(
                Wallet: wallet,
                Reservation: cancelledReservation,
                Transaction: transaction));

        return Result<ReservationOperationResponse>.Success(response);
    }
}
