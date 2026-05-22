using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Common;
using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;

namespace Wallet.Application.Wallets.Commands.TopUpWallet;

public class TopUpWalletCommandHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<TopUpWalletCommand, Result<WalletTransactionResultResponse>>
{
    public async Task<Result<WalletTransactionResultResponse>> Handle(TopUpWalletCommand request, CancellationToken ct)
    {
        var wallet = await context.UserWallets
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, ct);

        if (wallet == null)
        {
            wallet = new UserWallet(request.UserId);
            context.UserWallets.Add(wallet);
        }

        LedgerTransaction transaction;
        try
        {
            transaction = wallet.TopUp(amount: request.Amount, idem: request.IdempotencyKey);
            await context.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            return Result<WalletTransactionResultResponse>.Failure(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<WalletTransactionResultResponse>.Failure(WalletCommandErrors.ConcurrencyConflict);
        }

        var response = mapper.Map<WalletTransactionResultResponse>(
            new WalletTransactionMappingSource(Wallet: wallet, Transaction: transaction));

        return Result<WalletTransactionResultResponse>.Success(response);
    }
}
