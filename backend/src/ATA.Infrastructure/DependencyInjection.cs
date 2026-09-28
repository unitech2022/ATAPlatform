using ATA.Domain.Common;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Infrastructure.Push;
using ATA.Infrastructure.Security;
using ATA.Infrastructure.Sms;
using ATA.Infrastructure.Storage;
using ATA.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContext<AtaDbContext>(options => options.UseAtaMySql(connectionString));

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.Section));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.Section));
        services.AddSingleton<ISmsSender>(CreateSmsSender);

        services.Configure<OneSignalOptions>(configuration.GetSection(OneSignalOptions.Section));
        services.AddSingleton<IPushSender>(CreatePushSender);

        services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();

        services.AddScoped<DataSeeder>();
        return services;
    }

    /// <summary>A long-lived client with pooled-connection recycling (the recommended pattern without IHttpClientFactory).</summary>
    public static HttpClient CreateHttpClient() => new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

    /// <summary><c>OneSignalPushSender</c> only when <c>OneSignal:Enabled=true</c> and both keys are set; otherwise <c>LoggingPushSender</c>.</summary>
    private static IPushSender CreatePushSender(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<OneSignalOptions>>();
        if (options.Value.IsConfigured)
        {
            return new OneSignalPushSender(CreateHttpClient(), options, sp.GetRequiredService<ILogger<OneSignalPushSender>>());
        }

        return new LoggingPushSender(sp.GetRequiredService<ILogger<LoggingPushSender>>());
    }

    /// <summary><c>Sms:Sandbox=true</c> (default) or <c>Sms:Provider=logging</c> → <c>LoggingSmsSender</c> (no HTTP call).</summary>
    private static ISmsSender CreateSmsSender(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<SmsOptions>>();
        var sms = options.Value;
        if (!sms.Sandbox)
        {
            switch (sms.Provider.ToLowerInvariant())
            {
                case "unifonic" when !string.IsNullOrWhiteSpace(sms.Unifonic.AppSid):
                    return new UnifonicSmsSender(CreateHttpClient(), options);
                case "taqnyat" when !string.IsNullOrWhiteSpace(sms.Taqnyat.BearerToken):
                    return new TaqnyatSmsSender(CreateHttpClient(), options);
            }
        }

        return new LoggingSmsSender(sp.GetRequiredService<ILogger<LoggingSmsSender>>());
    }
}
