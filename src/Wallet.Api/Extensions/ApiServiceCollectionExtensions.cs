using Wallet.Api.Common;
using Wallet.Api.Interceptors;
using Wallet.Api.OpenApi;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;

namespace Wallet.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
            options.AddOperationTransformer<IdempotencyKeyHeaderOperationTransformer>());

        services.AddHttpContextAccessor();
        services.AddScoped<IRouteInfo, HttpContextRouteInfo>();
        services.AddScoped<IRequestHasher, RequestHasher>();
        services.AddGrpc(options => options.Interceptors.Add<GrpcIdempotencyInterceptor>());

        return services;
    }
}
