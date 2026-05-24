namespace Wallet.Application.Abstractions.Contracts;

public interface IIdempotentResponse
{
    string GetResponseBody();
}