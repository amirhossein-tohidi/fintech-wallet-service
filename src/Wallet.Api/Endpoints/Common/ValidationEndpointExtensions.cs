using FluentValidation;
using Wallet.Api.Constants;

namespace Wallet.Api.Endpoints.Common;

public static class ValidationEndpointExtensions
{
    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
            if (validator == null)
            {
                return await next(context);
            }

            var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
            if (request == null)
            {
                return await next(context);
            }

            var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
            if (validationResult.IsValid)
            {
                return await next(context);
            }

            context.HttpContext.Items[HttpContextItemKeys.SkipIdempotencyPersistence] = true;

            return Results.ValidationProblem(validationResult.ToDictionary());
        });
    }
}
