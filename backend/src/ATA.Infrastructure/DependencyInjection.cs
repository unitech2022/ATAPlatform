using ATA.Domain.Common;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Infrastructure.Security;
using ATA.Infrastructure.Sms;
using ATA.Infrastructure.Storage;
using ATA.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddSingleton<ISmsSender, LoggingSmsSender>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();

        services.AddScoped<DataSeeder>();
        return services;
    }
}
