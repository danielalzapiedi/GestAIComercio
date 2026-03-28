using GestAI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestAI.Api.Middleware;

public sealed class AppResultHttpMappingFilter : IAsyncResultFilter
{
    private const string CorrelationHeader = "X-Correlation-ID";

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is not ObjectResult objectResult || objectResult.StatusCode is not null and not 200)
            return next();

        if (!TryMapFailure(objectResult.Value, out var failure))
            return next();

        var correlationId = context.HttpContext.Response.Headers[CorrelationHeader].FirstOrDefault()
                            ?? context.HttpContext.Request.Headers[CorrelationHeader].FirstOrDefault()
                            ?? context.HttpContext.TraceIdentifier;

        var statusCode = ResolveStatusCode(failure.ErrorCode);
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Operation failed",
            Detail = BusinessErrorCatalog.ResolveMessage(failure.ErrorCode, failure.Message),
            Extensions =
            {
                ["errorCode"] = failure.ErrorCode,
                ["correlationId"] = correlationId
            }
        };

        context.Result = new ObjectResult(problem) { StatusCode = statusCode };
        return next();
    }

    private static bool TryMapFailure(object? value, out FailureData failure)
    {
        failure = default;
        switch (value)
        {
            case AppResult result when !result.Success:
                failure = new FailureData(result.ErrorCode, result.Message);
                return true;
            case null:
                return false;
        }

        var type = value.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(AppResult<>))
            return false;

        var success = (bool?)type.GetProperty(nameof(AppResult.Success))?.GetValue(value);
        if (success != false)
            return false;

        var errorCode = type.GetProperty(nameof(AppResult.ErrorCode))?.GetValue(value) as string;
        var message = type.GetProperty(nameof(AppResult.Message))?.GetValue(value) as string;
        failure = new FailureData(errorCode, message);
        return true;
    }

    private static int ResolveStatusCode(string? errorCode)
    {
        var normalized = errorCode?.Trim().ToLowerInvariant();
        return normalized switch
        {
            BusinessErrorCatalog.Forbidden => StatusCodes.Status403Forbidden,
            BusinessErrorCatalog.Unauthorized => StatusCodes.Status401Unauthorized,
            BusinessErrorCatalog.NotFound => StatusCodes.Status404NotFound,
            BusinessErrorCatalog.Duplicate or BusinessErrorCatalog.DuplicateCode => StatusCodes.Status409Conflict,
            BusinessErrorCatalog.ValidationError or "invalid_parent" or "invalid_extension" or "file_too_large" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private readonly record struct FailureData(string? ErrorCode, string? Message);
}
