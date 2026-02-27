using MediatR;
using Microsoft.Extensions.Logging;

namespace Security.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs request/response timings and any unhandled exceptions.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling {RequestName}", requestName);
            throw;
        }
    }
}
