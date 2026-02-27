using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Security.Web.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            context.Response.ContentType = "application/problem+json";
            var env = context.RequestServices.GetService<IWebHostEnvironment>();
            var detail = env?.IsDevelopment() == true ? exception.Message : "An unexpected error occurred.";
            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "An unexpected error occurred",
                status = 500,
                detail
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        else if (!context.Response.HasStarted)
        {
            context.Response.Redirect("/Error");
        }
    }
}
