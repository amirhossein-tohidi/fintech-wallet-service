using Wallet.Api.Common;
using Wallet.Application.Abstractions;
using Wallet.Application.Common;

namespace Wallet.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();

        services.AddHttpContextAccessor();
        services.AddScoped<IRouteInfo, HttpContextRouteInfo>();
        services.AddScoped<IRequestHasher, RequestHasher>();

        return services;
    }
}
