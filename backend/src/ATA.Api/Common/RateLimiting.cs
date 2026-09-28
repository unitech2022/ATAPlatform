using System.Threading.RateLimiting;
using ATA.Domain.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace ATA.Api.Common;

public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";
    /// <summary>OTP requests allowed per client IP per hour.</summary>
    public int OtpPerIpPerHour { get; set; } = 10;
}

public static class RateLimiting
{
    public const string OtpPolicy = "otp";

    public static IServiceCollection AddAtaRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.Section).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(OtpPolicy, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.OtpPerIpPerHour,
                        Window = TimeSpan.FromHours(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var after) ? (int)after.TotalSeconds : 60;
                await context.HttpContext.WriteErrorAsync(ErrorCodes.RateLimited, context.HttpContext.GetLanguage(), new { retryAfterSeconds = retryAfter }, cancellationToken);
            };
        });
        return services;
    }
}
