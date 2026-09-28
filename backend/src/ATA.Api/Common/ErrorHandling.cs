using System.Text.Json;
using ATA.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace ATA.Api.Common;

/// <summary>Converts every unhandled exception into the <c>{ "error": { code, message, details } }</c> envelope.</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var language = httpContext.GetLanguage();
        var (code, details) = exception switch
        {
            DomainException domain => (domain.Code, domain.Details),
            BadHttpRequestException bad => (ErrorCodes.ValidationFailed, (object?)new { body = bad.Message }),
            JsonException json => (ErrorCodes.ValidationFailed, new { body = json.Message }),
            _ => (ErrorCodes.InternalError, null),
        };

        if (code == ErrorCodes.InternalError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        await httpContext.WriteErrorAsync(code, language, details, cancellationToken);
        return true;
    }
}

public static class ErrorResponses
{
    public static async Task WriteErrorAsync(this HttpContext context, string code, Language language, object? details = null, CancellationToken cancellationToken = default)
    {
        var status = ErrorCatalog.StatusOf(code);
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        if (status == StatusCodes.Status429TooManyRequests && details is not null)
        {
            var retry = JsonSerializer.SerializeToElement(details);
            if (retry.ValueKind == JsonValueKind.Object && retry.TryGetProperty("retryAfterSeconds", out var seconds))
            {
                context.Response.Headers.RetryAfter = seconds.ToString();
            }
        }

        var options = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        await context.Response.WriteAsJsonAsync(ErrorCatalog.Envelope(code, language, details), options, cancellationToken);
    }
}
