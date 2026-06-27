using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wallet.Application.Common.Behaviors;

/// <summary>
/// Handles common exceptions in command handlers: validation errors, business rule violations, and concurrency conflicts.
/// Eliminates duplicate try/catch boilerplate in all command handlers.
/// </summary>
public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ExceptionHandlingBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Run FluentValidation first (if any validators registered for this request)
        var validationContext = new ValidationContext<TRequest>(request);
        var validationFailures = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(validationContext, cancellationToken)));

        var failures = validationFailures
            .Where(vr => !vr.IsValid)
            .SelectMany(vr => vr.Errors)
            .ToList();

        if (failures.Any())
        {
            // Return validation errors as Failure result if TResult is Result<T>
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Wallet.Application.Common.Result<>))
            {
                var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
                return (TResponse)(object)Wallet.Application.Common.Result.Failure(errorMessage);
            }
        }

        try
        {
            return await next();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient balance") ||
                                                  ex.Message.Contains("Reservation cannot") ||
                                                  ex.Message.Contains("Cannot cancel") ||
                                                  ex.Message.Contains("Reservation not eligible") ||
                                                  ex.Message.Contains("Insufficient promo"))
        {
            // Business rule violations - return as Failure
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Wallet.Application.Common.Result<>))
            {
                return (TResponse)(object)Wallet.Application.Common.Result.Failure(ex.Message);
            }
            throw; // rethrow if not a Result type
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrency conflicts - return standard error
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Wallet.Application.Common.Result<>))
            {
                return (TResponse)(object)Wallet.Application.Common.Result.Failure(Wallet.Application.Common.WalletCommandErrors.ConcurrencyConflict);
            }
            throw; // rethrow if not a Result type
        }
        catch (InvalidOperationException ex)
        {
            // Other business rule violations
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Wallet.Application.Common.Result<>))
            {
                return (TResponse)(object)Wallet.Application.Common.Result.Failure(ex.Message);
            }
            throw; // rethrow if not a Result type
        }
    }
}