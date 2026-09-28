using ATA.Domain.Common;
using ATA.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ATA.Infrastructure.Persistence;

/// <summary>Used only by <c>dotnet ef</c> at design time; no database connection is opened for migrations scaffolding.</summary>
public sealed class AtaDbContextFactory : IDesignTimeDbContextFactory<AtaDbContext>
{
    public AtaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AtaDbContext>()
            .UseAtaMySql("Server=localhost;Port=3306;Database=ata;User=ata;Password=ata;")
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
