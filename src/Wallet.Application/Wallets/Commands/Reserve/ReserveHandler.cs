using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Common;
using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;

namespace Wallet.Application.Wallets.Commands.Reserve;

public class ReserveHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<ReserveCommand, Result<ReservationOperationResponse>>
{
    public async Task<Result<ReservationOperationResponse>> Handle(ReserveCommand request, CancellationToken ct)
    {
        var wallet = await context.UserWallets
            .FirstOrDefaultAsync(x => x.Id == request.WalletId, ct);

        if (wallet == null)
        {
            return Result<ReservationOperationResponse>.Failure("Wallet not found.");
        }

        Reservation reservation;
        try
        {
            reservation = wallet.CreateReservation(
                serviceType: request.ServiceType,
                amount: request.Amount,
                expireAt: DateTime.UtcNow.AddMinutes(9),
                idem: request.IdempotencyKey);
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

        var response = mapper.Map<ReservationOperationResponse>(
            new ReservationOperationMappingSource(Wallet: wallet, Reservation: reservation, Transaction: null));

        return Result<ReservationOperationResponse>.Success(response);
    }
}
