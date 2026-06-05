using Wallet.Application.Abstractions;

namespace Wallet.Api.Grpc.Common;

public sealed class GrpcRouteInfo(string method) : IRouteInfo
{
    public string HttpMethod => "GRPC";
    public string Path => method;
}
