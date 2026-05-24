using Wallet.Application.Abstractions;

namespace Wallet.UnitTests.Application;

internal sealed class TestRouteInfo : IRouteInfo
{
    public string HttpMethod => "POST";
    public string Path => "/unit-test";
}
