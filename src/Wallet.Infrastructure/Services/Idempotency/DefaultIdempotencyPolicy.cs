using Wallet.Application.Abstractions;

namespace Wallet.Infrastructure.Services.Idempotency;

public class DefaultIdempotencyPolicy : IIdempotencyPolicy
{
    public TimeSpan GetExpiration() => TimeSpan.FromDays(1);
}
