using System.Globalization;
using FluentValidation;
using Grpc.Core;
using MediatR;
using Wallet.Api.Constants;
using Wallet.Api.Grpc.Common;
using Wallet.Application.Abstractions;
using Wallet.Contracts.Requests;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Wallet;

public sealed partial class WalletGrpcService : WalletGrpc.WalletGrpcBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<TopUpWalletRequest> _topUpValidator;
    private readonly IValidator<FastPayRequest> _fastPayValidator;
    private readonly IValidator<RefundRequest> _refundValidator;
    private readonly IValidator<ReserveRequest> _reserveValidator;
    private readonly IValidator<AddPromoGrantRequest> _addPromoGrantValidator;
    private readonly IValidator<ConsumePromoRequest> _consumePromoValidator;

    public WalletGrpcService(
        IMediator mediator,
        IValidator<TopUpWalletRequest> topUpValidator,
        IValidator<FastPayRequest> fastPayValidator,
        IValidator<RefundRequest> refundValidator,
        IValidator<ReserveRequest> reserveValidator,
        IValidator<AddPromoGrantRequest> addPromoGrantValidator,
        IValidator<ConsumePromoRequest> consumePromoValidator)
    {
        _mediator = mediator;
        _topUpValidator = topUpValidator;
        _fastPayValidator = fastPayValidator;
        _refundValidator = refundValidator;
        _reserveValidator = reserveValidator;
        _addPromoGrantValidator = addPromoGrantValidator;
        _consumePromoValidator = consumePromoValidator;
    }

    private static decimal ParseAmount(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : throw GrpcExceptionMapper.ValidationFailed("Amount must be a valid decimal string.");
    }

    private static string GetIdempotencyKey(ServerCallContext context)
    {
        return context.RequestHeaders
                   .FirstOrDefault(header => string.Equals(header.Key, HeaderNames.IdempotencyKey, StringComparison.OrdinalIgnoreCase))
                   ?.Value
               ?? string.Empty;
    }

    private static IRouteInfo GetRouteInfo(ServerCallContext context)
    {
        return new GrpcRouteInfo(context.Method);
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw GrpcExceptionMapper.ValidationFailed(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage)));
        }
    }
}
