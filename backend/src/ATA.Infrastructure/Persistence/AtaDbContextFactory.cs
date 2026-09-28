using ATA.Domain.Common;
using ATA.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ATA.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time. Scaffolding opens no connection; <c>database update</c>
/// targets <c>ConnectionStrings__Default</c> when set, otherwise the local development database.
/// </summary>
public sealed class AtaDbContextFactory : IDesignTimeDbContextFactory<AtaDbContext>
{
    private const string DevelopmentConnectionString = "Server=localhost;Port=3306;Database=ata;User=ata;Password=ata;";

    public AtaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        var options = new DbContextOptionsBuilder<AtaDbContext>()
            .UseAtaMySql(string.IsNullOrWhiteSpace(connectionString) ? DevelopmentConnectionString : connectionString)
            .Options;
        return new AtaDbContext(options, new SystemClock());
    }
}

public static class MySqlOptionsExtensions
{
    public static readonly ServerVersion MySqlServerVersion = ServerVersion.Parse("8.0.36-mysql");

    public static DbContextOptionsBuilder<AtaDbContext> UseAtaMySql(this DbContextOptionsBuilder<AtaDbContext> builder, string connectionString)
    {
        builder.UseMySql(connectionString, MySqlServerVersion, mysql =>
        {
            mysql.MigrationsAssembly(typeof(AtaDbContext).Assembly.FullName);
            mysql.EnableRetryOnFailure(3);
        });
        return builder;
    }

    public static DbContextOptionsBuilder UseAtaMySql(this DbContextOptionsBuilder builder, string connectionString)
    {
        builder.UseMySql(connectionString, MySqlServerVersion, mysql =>
        {
            mysql.MigrationsAssembly(typeof(AtaDbContext).Assembly.FullName);
            mysql.EnableRetryOnFailure(3);
        });
        return builder;
    }
}
