using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Catalog;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Files;
using ATA.Api.Modules.Identity;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Passengers;
using ATA.Api.Modules.Wallet;
using ATA.Domain.Common;
using ATA.Infrastructure;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAtaAuthentication(builder.Configuration);
builder.Services.AddAtaRateLimiting(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.Configure<JsonOptions>(o => JsonDefaults.Configure(o.SerializerOptions));
builder.Services.AddOpenApi("v1");
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("mysql");

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Retry-After", "Content-Disposition")));

builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.Section));
builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.Section));
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MeService>();
builder.Services.AddScoped<PassengerService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<DriverApplicationService>();
builder.Services.AddScoped<DriverStatusService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminDriverService>();
builder.Services.AddScoped<AdminService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(http => http.WriteErrorAsync(ErrorCodes.InternalError, http.GetLanguage())));
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference("/docs", o => o.WithTitle("ATA Platform API").WithOpenApiRoutePattern("/openapi/{documentName}.json"));
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
    },
});

var api = app.MapGroup("/api/v1");
IdentityEndpoints.Map(api);
CatalogEndpoints.Map(api);
PassengerEndpoints.Map(api);
WalletEndpoints.Map(api);
DriverEndpoints.Map(api);
NotificationEndpoints.Map(api);
FileEndpoints.Map(api);
AdminEndpoints.Map(api);
api.MapFallback((HttpContext http) => http.WriteErrorAsync(ErrorCodes.NotFound, http.GetLanguage()));

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtaDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
}

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory</c> in the test project.</summary>
public partial class Program;
