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

        // F20 (doc 12 §F20.5): AuthResponse (+ mustChangePassword) | { mfaRequired, mfaToken, methods } | { mfaEnrollmentRequired, mfaToken }.
        auth.MapPost("/admin/login", async (AdminLoginRequest request, AdminAuthService service, CancellationToken ct) =>
                Results.Ok(await service.LoginAsync(request, ct)))
            .Produces<AuthResponse>()
            .Produces<AdminMfaChallengeResponse>()
            .Produces<AdminMfaEnrollmentResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/admin/mfa/verify", async (AdminMfaVerifyRequest request, AdminAuthService service, CancellationToken ct) =>
                Results.Ok(await service.VerifyAsync(request, ct)))
            .Produces<AuthResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/admin/mfa/enroll", async (AdminMfaEnrollRequest request, AdminAuthService service, CancellationToken ct) =>
                Results.Ok(await service.EnrollAsync(request, ct)))
            .Produces<AdminMfaEnrollResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status401Unauthorized);

        auth.MapPost("/admin/mfa/enroll/confirm", async (AdminMfaEnrollConfirmRequest request, AdminAuthService service, CancellationToken ct) =>
                Results.Ok(await service.ConfirmEnrollmentAsync(request, ct)))
            .Produces<AdminMfaEnrollConfirmResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);

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
