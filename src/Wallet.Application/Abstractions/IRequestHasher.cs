namespace Wallet.Application.Abstractions;

public interface IRequestHasher
{
    string ComputeHash<T>(T request);
}