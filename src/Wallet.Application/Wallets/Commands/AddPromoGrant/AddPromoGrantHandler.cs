using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Common;
using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;

namespace Wallet.Application.Wallets.Commands.AddPromoGrant;

public class AddPromoGrantHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<AddPromoGrantCommand, Result<PromoGrantOperationResponse>>
{
    public async Task<Result<PromoGrantOperationResponse>> Handle(AddPromoGrantCommand request, CancellationToken ct)
    {
        var wallet = await context.UserWallets
            .FirstOrDefaultAsync(x => x.Id == request.WalletId, ct);

        if (wallet == null)
        {
            return Result<PromoGrantOperationResponse>.Failure("Wallet not found.");
        }

        PromoGrant promoGrant;
        try
        {
            promoGrant = wallet.AddPromoGrant(
                serviceType: request.ServiceType,
                amount: request.Amount,
                expiresAt: request.ExpiresAt);
            await context.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<PromoGrantOperationResponse>.Failure(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PromoGrantOperationResponse>.Failure(WalletCommandErrors.ConcurrencyConflict);
        }

        var response = mapper.Map<PromoGrantOperationResponse>(
            new PromoGrantMappingSource(Wallet: wallet, PromoGrant: promoGrant));

        return Result<PromoGrantOperationResponse>.Success(response);
    }
}
