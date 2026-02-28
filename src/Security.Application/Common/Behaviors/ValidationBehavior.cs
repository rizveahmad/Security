using FluentValidation;
using MediatR;
using Security.Application.Common.Models;

namespace Security.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that runs FluentValidation validators before the request handler.
/// Returns a failed Result when one or more validators report errors.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(e => e != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // When the response is Result or Result<T>, return a typed failure instead of throwing.
        var errors = failures.Select(f => f.ErrorMessage).ToArray();

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(errors);

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod(
                nameof(Result.Failure),
                [typeof(string[])]);

            if (failureMethod != null)
                return (TResponse)failureMethod.Invoke(null, [errors])!;
        }

        throw new ValidationException(failures);
    }
}
