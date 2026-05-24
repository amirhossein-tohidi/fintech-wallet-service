using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using Wallet.Api.Constants;
using Wallet.Api.Endpoints.Common;

namespace Wallet.Api.OpenApi;

public sealed class IdempotencyKeyHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken ct)
    {
        var requiresIdempotencyKey = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<RequireIdempotencyKeyMetadata>()
            .Any();

        if (!requiresIdempotencyKey)
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];

        var alreadyDefined = operation.Parameters.Any(parameter =>
            string.Equals(parameter.Name, HeaderNames.IdempotencyKey, StringComparison.OrdinalIgnoreCase) &&
            parameter.In == ParameterLocation.Header);

        if (alreadyDefined)
        {
            return Task.CompletedTask;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HeaderNames.IdempotencyKey,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Unique idempotency key for safe retries. Use a new value for each new financial operation. e.g a(UUID) => 00000000-0000-4000-8000-000000000000.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });

        return Task.CompletedTask;
    }
}