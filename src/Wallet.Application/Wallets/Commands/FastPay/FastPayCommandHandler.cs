using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Wallet.Application.Abstractions.Persistence;
using Wallet.Application.Common;
using Wallet.Application.Mapping;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;

namespace Wallet.Application.Wallets.Commands.FastPay;

public class FastPayCommandHandler(IApplicationDbContext context, IMapper mapper)
    : IRequestHandler<FastPayCommand, Result<WalletTransactionResultResponse>>
{
    public async Task<Result<WalletTransactionResultResponse>> Handle(FastPayCommand request, CancellationToken ct)
    {
        var wallet = await context.UserWallets
            .FirstOrDefaultAsync(x => x.Id == request.WalletId, ct);

        if (wallet == null)
        {
            return Result<WalletTransactionResultResponse>.Failure("Wallet not found.");
        }

        LedgerTransaction transaction;
        try
        {
            transaction = wallet.Pay(
                serviceType: request.ServiceType,
                amount: request.Amount,
                idem: request.IdempotencyKey);
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
