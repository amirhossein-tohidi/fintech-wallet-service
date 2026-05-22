namespace Wallet.Application.Abstractions;

public interface IRouteInfo
{
    string HttpMethod { get; }
    string Path { get; }
    string Endpoint => $"{HttpMethod} {Path}";
}