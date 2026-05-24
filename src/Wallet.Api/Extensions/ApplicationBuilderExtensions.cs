using Scalar.AspNetCore;
using Wallet.Api.Endpoints;
using Wallet.Api.Middlewares;

namespace Wallet.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication ConfigureRequestPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapWalletEndpoints();

        return app;
    }
}
