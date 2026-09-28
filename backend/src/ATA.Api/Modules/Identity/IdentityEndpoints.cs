using ATA.Api.Common;

namespace ATA.Api.Modules.Identity;

public static class IdentityEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/otp/request", async (OtpRequestRequest request, AuthService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.RequestOtpAsync(request, http.GetLanguage(), ct)))
            .RequireRateLimiting(RateLimiting.OtpPolicy)
            .Produces<OtpRequestResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/otp/verify", async (OtpVerifyRequest request, AuthService service, CancellationToken ct) =>
                Results.Ok(await service.VerifyOtpAsync(request, ct)))
            .RequireRateLimiting(RateLimiting.OtpPolicy)
            .Produces<AuthResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

        auth.MapPost("/refresh", async (RefreshRequest request, AuthService service, CancellationToken ct) =>
                Results.Ok(await service.RefreshAsync(request, ct)))
            .Produces<AuthResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", async (LogoutRequest request, AuthService service, CancellationToken ct) =>
            {
                await service.LogoutAsync(request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        auth.MapPost("/admin/login", async (AdminLoginRequest request, AuthService service, CancellationToken ct) =>
                Results.Ok(await service.AdminLoginAsync(request, ct)))
            .Produces<AuthResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

        var me = api.MapGroup("/me").WithTags("Me").RequireAuthorization(Policies.Authenticated);

        me.MapGet("/", async (MeService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)))
            .Produces<MeResponse>();

        me.MapPatch("/", async (UpdateMeRequest request, MeService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(request, ct)))
            .Produces<UserDto>();

        me.MapGet("/notification-preferences", async (MeService service, CancellationToken ct) =>
                Results.Ok(await service.GetNotificationPreferencesAsync(ct)))
            .Produces<NotificationPreferencesDto>();

        me.MapPut("/notification-preferences", async (NotificationPreferencesDto request, MeService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateNotificationPreferencesAsync(request, ct)))
            .Produces<NotificationPreferencesDto>();

        me.MapPut("/devices", async (RegisterDeviceRequest request, MeService service, CancellationToken ct) =>
            {
                await service.RegisterDeviceAsync(request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        me.MapDelete("/", async (MeService service, CancellationToken ct) =>
            {
                await service.DeleteAccountAsync(ct);
                return Results.Accepted();
            })
            .Produces(StatusCodes.Status202Accepted);
    }
}
