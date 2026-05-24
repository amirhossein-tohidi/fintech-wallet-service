using FluentValidation;
using Wallet.Contracts.Requests;

namespace Wallet.Application.Validation;

public sealed class TopUpWalletRequestValidator : AbstractValidator<TopUpWalletRequest>
{
    public TopUpWalletRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class FastPayRequestValidator : AbstractValidator<FastPayRequest>
{
    public FastPayRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class ReserveRequestValidator : AbstractValidator<ReserveRequest>
{
    public ReserveRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class RefundRequestValidator : AbstractValidator<RefundRequest>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class AddPromoGrantRequestValidator : AbstractValidator<AddPromoGrantRequest>
{
    public AddPromoGrantRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => expiresAt > DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future.");
    }
}

public sealed class ConsumePromoRequestValidator : AbstractValidator<ConsumePromoRequest>
{
    public ConsumePromoRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
