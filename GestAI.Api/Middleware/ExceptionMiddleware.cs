using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GestAI.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = EnsureCorrelationId(context);
        try
        {
            await next(context);
        }
        catch (ValidationException vex)
        {
            logger.LogWarning(vex, "Validation error. CorrelationId: {CorrelationId}", correlationId);

            var details = new ValidationProblemDetails(
                vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Extensions =
                {
                    ["errorCode"] = "validation_error",
                    ["correlationId"] = correlationId
                }
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(details);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error. CorrelationId: {CorrelationId}", correlationId);

            var details = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error",
                Detail = "Ocurrió un error inesperado.",
                Extensions =
                {
                    ["errorCode"] = "server_error",
                    ["correlationId"] = correlationId
                }
            };

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(details);
        }
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        var headerValue = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(headerValue) ? context.TraceIdentifier : headerValue.Trim();

        context.Response.Headers[CorrelationHeader] = correlationId;
        return correlationId;
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
