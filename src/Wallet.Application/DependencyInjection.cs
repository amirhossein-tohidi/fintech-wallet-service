using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Wallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            // Add the exception handling behavior - it will be applied to all requests
            // MediatR automatically picks up any IPipelineBehavior<TRequest, TResponse> 
            // that are registered in the DI container
        });

        // Register our custom behavior - it will be applied as a pipeline behavior
        // to all MediatR handlers
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(_ => { }, assembly);

        return services;
    }
}