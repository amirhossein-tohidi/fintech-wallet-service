namespace Wallet.Application.Abstractions;

public interface IIdempotencyPolicy
{
    TimeSpan GetExpiration();
}