using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Cancellation;
using ATA.Api.Modules.Catalog;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Files;
using ATA.Api.Modules.Identity;
using ATA.Api.Modules.Incentives;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Passengers;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Promotions;
using ATA.Api.Modules.Ratings;
using ATA.Api.Modules.Safety;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Matching;
using ATA.Api.Modules.Trips.Realtime;
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
builder.Services.AddDataProtection();
builder.Services.AddSignalR().AddJsonProtocol(o => JsonDefaults.Configure(o.PayloadSerializerOptions));

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("Retry-After", "Content-Disposition")));

builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.Section));
builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.Section));
builder.Services.Configure<PayoutsOptions>(builder.Configuration.GetSection(PayoutsOptions.Section));
builder.Services.Configure<SettlementsOptions>(builder.Configuration.GetSection(SettlementsOptions.Section));
builder.Services.Configure<NotificationsOptions>(builder.Configuration.GetSection(NotificationsOptions.Section));
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MeService>();
builder.Services.AddScoped<PassengerService>();
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<DriverApplicationService>();
builder.Services.AddScoped<DriverStatusService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<NotificationTemplateCache>();
builder.Services.AddSingleton<DeliveryQueue>();
builder.Services.AddScoped<NotificationOutbox>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<NotificationDeliveryProcessor>();
builder.Services.AddScoped<NotificationAdminService>();
builder.Services.AddScoped<AudienceResolver>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<DocumentExpiryScanner>();
builder.Services.AddSingleton<SandboxGateway>();
builder.Services.AddSingleton<IPaymentGateway>(sp => sp.GetRequiredService<SandboxGateway>());
builder.Services.AddSingleton<IPaymentGateway>(sp => new MoyasarGateway(ATA.Infrastructure.DependencyInjection.CreateHttpClient(), sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentsOptions>>()));
builder.Services.AddSingleton<IPaymentGatewayResolver, PaymentGatewayResolver>();
builder.Services.AddScoped<LedgerService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PaymentMethodService>();
builder.Services.AddScoped<CardTripPaymentService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<PayoutService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<AdminPaymentService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<DriverEarningsService>();
builder.Services.AddScoped<PaymentJobs>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminDriverService>();
builder.Services.AddScoped<AdminService>();

builder.Services.Configure<TripOptions>(builder.Configuration.GetSection(TripOptions.Section));
builder.Services.Configure<MatchingOptions>(builder.Configuration.GetSection(MatchingOptions.Section));
builder.Services.Configure<RealtimeOptions>(builder.Configuration.GetSection(RealtimeOptions.Section));
builder.Services.Configure<PricingOptions>(builder.Configuration.GetSection(PricingOptions.Section));
builder.Services.Configure<DemandOptions>(builder.Configuration.GetSection(DemandOptions.Section));
builder.Services.AddSingleton<FlatPricing>();
builder.Services.AddScoped<IPricingService, RulePricingService>();
builder.Services.AddSingleton<ZoneCache>();
builder.Services.AddScoped<ZoneResolver>();
builder.Services.AddSingleton<DemandCache>();
builder.Services.AddScoped<DemandService>();
builder.Services.AddScoped<DemandComputer>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<PricingAdminService>();
builder.Services.AddSingleton<ITripNotifier, SignalRTripNotifier>();
builder.Services.AddSingleton<MatchingSettingsCache>();
builder.Services.AddScoped<MatchingSettingsProvider>();
builder.Services.AddScoped<IFavoriteDriverProvider, NoFavoriteDrivers>();
builder.Services.AddScoped<CounterDriverReliability>();
builder.Services.AddScoped<IDriverReliabilityProvider, ProfileDriverReliability>();
builder.Services.AddScoped<IMatcher, ScoringMatcher>();
builder.Services.AddScoped<MatchingRecorder>();
builder.Services.AddScoped<MatchingAdminService>();
builder.Services.AddScoped<TripPinService>();
builder.Services.AddScoped<TripEventRecorder>();
builder.Services.AddScoped<TripNumberGenerator>();
builder.Services.AddScoped<TripReadService>();
builder.Services.AddScoped<TripPaymentService>();
builder.Services.AddScoped<PassengerTripService>();
builder.Services.AddScoped<DriverTripService>();
builder.Services.AddScoped<AdminTripService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.Configure<SafetyOptions>(builder.Configuration.GetSection(SafetyOptions.Section));
builder.Services.PostConfigure<SafetyOptions>(o => o.TripMessagesRetentionDays = builder.Configuration.GetValue("Retention:TripMessagesDays", o.TripMessagesRetentionDays));
builder.Services.Configure<CallMaskingOptions>(builder.Configuration.GetSection(CallMaskingOptions.Section));
builder.Services.Configure<CancellationOptions>(builder.Configuration.GetSection(CancellationOptions.Section));
builder.Services.Configure<ReliabilityOptions>(builder.Configuration.GetSection(ReliabilityOptions.Section));
builder.Services.AddSingleton<ISafetyNotifier, SignalRSafetyNotifier>();
builder.Services.AddSingleton<ShareViewThrottle>();
builder.Services.AddSingleton<ICallMaskingProvider, NoneCallMaskingProvider>();
builder.Services.AddScoped<SafetyCaseFactory>();
builder.Services.AddScoped<TripShareService>();
builder.Services.AddScoped<TrustedContactService>();
builder.Services.AddScoped<SafetyService>();
builder.Services.AddScoped<TripChatService>();
builder.Services.AddScoped<LostItemService>();
builder.Services.AddScoped<AdminSafetyService>();
builder.Services.AddScoped<SafetyMonitor>();
builder.Services.AddScoped<ReliabilityService>();
builder.Services.AddScoped<IReliabilityService>(sp => sp.GetRequiredService<ReliabilityService>());
builder.Services.AddScoped<CancellationEngine>();
builder.Services.AddScoped<TripCancellationService>();
builder.Services.AddScoped<CancellationAdminService>();
builder.Services.Configure<RatingsOptions>(builder.Configuration.GetSection(RatingsOptions.Section));
builder.Services.Configure<PromotionsOptions>(builder.Configuration.GetSection(PromotionsOptions.Section));
builder.Services.Configure<TiersOptions>(builder.Configuration.GetSection(TiersOptions.Section));
builder.Services.Configure<IncentivesOptions>(builder.Configuration.GetSection(IncentivesOptions.Section));
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<RatingAdminService>();
builder.Services.AddScoped<IDiscountEngine, DiscountEngine>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<PromotionAdminService>();
builder.Services.AddSingleton<DriverTierRuleCache>();
builder.Services.AddScoped<TierRuleProvider>();
builder.Services.AddScoped<TierService>();
builder.Services.AddScoped<IncentiveService>();
builder.Services.AddScoped<IncentiveAdminService>();
builder.Services.AddHostedService<MatchingBackgroundService>();
builder.Services.AddHostedService<DemandBackgroundService>();
builder.Services.AddHostedService<LiveSnapshotService>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();
builder.Services.AddHostedService<NotificationJobsBackgroundService>();
builder.Services.AddHostedService<PaymentsBackgroundService>();
builder.Services.AddHostedService<SafetyBackgroundService>();
builder.Services.AddHostedService<ReliabilityBackgroundService>();
builder.Services.AddHostedService<RatingsBackgroundService>();
builder.Services.AddHostedService<IncentivesBackgroundService>();

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
TripEndpoints.Map(api);
PricingEndpoints.Map(api);
MatchingEndpoints.Map(api);
NotificationEndpoints.Map(api);
NotificationAdminEndpoints.Map(api);
PaymentEndpoints.Map(api);
SafetyEndpoints.Map(api);
CancellationEndpoints.Map(api);
RatingEndpoints.Map(api);
PromotionEndpoints.Map(api);
IncentiveEndpoints.Map(api);
FileEndpoints.Map(api);
AdminEndpoints.Map(api);
api.MapFallback((HttpContext http) => http.WriteErrorAsync(ErrorCodes.NotFound, http.GetLanguage()));
app.MapHub<TripsHub>("/hubs/trips");

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
