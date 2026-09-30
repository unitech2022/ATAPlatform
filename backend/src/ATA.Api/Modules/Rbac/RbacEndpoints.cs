using ATA.Api.Common;
using ATA.Api.Modules.Identity;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Rbac;

public static class RbacEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin users & roles").RequireAuthorization(Policies.Admin);

        // ----- /admin/me (any admin) -----
        var me = admin.MapGroup("/me").AllowAnyAdmin();
        me.MapGet("/", async (AdminSelfService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.GetAsync(http.GetLanguage(), ct)))
            .Produces<AdminMeResponse>();
        me.MapPost("/password", async (ChangePasswordRequest request, AdminSelfService service, CancellationToken ct) =>
            {
                await service.ChangePasswordAsync(request, ct);
                return Results.NoContent();
            })
            .AllowDuringPasswordChange()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        me.MapPost("/mfa/recovery-codes", async (RecoveryCodesRequest request, AdminSelfService service, CancellationToken ct) =>
                Results.Ok(await service.RegenerateRecoveryCodesAsync(request, ct)))
            .Produces<RecoveryCodesResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);
        // Dashboard addition: voluntary MFA enrolment of a signed-in admin (doc 12 only enrols at login).
        me.MapPost("/mfa/enroll", async (AdminSelfService service, CancellationToken ct) => Results.Ok(await service.StartEnrollmentAsync(ct)))
            .Produces<AdminMfaEnrollResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        me.MapPost("/mfa/enroll/confirm", async (RecoveryCodesRequest request, AdminSelfService service, CancellationToken ct) =>
                Results.Ok(await service.ConfirmEnrollmentAsync(request, ct)))
            .Produces<RecoveryCodesResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest);
        me.MapGet("/sessions", async (AdminSelfService service, CancellationToken ct) => Results.Ok(await service.SessionsAsync(ct)))
            .Produces<List<AdminSessionDto>>();
        me.MapDelete("/sessions/{id:guid}", async (Guid id, AdminSelfService service, CancellationToken ct) =>
            {
                await service.RevokeSessionAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        me.MapPost("/sessions/revoke-others", async (AdminSelfService service, CancellationToken ct) =>
            {
                await service.RevokeOtherSessionsAsync(ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // ----- admin users (admin.users.manage) -----
        var users = admin.MapGroup("/admin-users").RequirePermission(Permissions.AdminUsersManage);
        users.MapGet("/", async (string? search, Guid? roleId, bool? isActive, int? page, int? pageSize, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(search, roleId, isActive, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<AdminUserDto>>();
        users.MapGet("/{id:guid}", async (Guid id, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminUserDetailDto>();
        users.MapPost("/", async (CreateAdminUserRequest request, AdminUserService service, HttpContext http, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/admin/admin-users/{created.AdminUser.Id}", created);
            })
            .Produces<CreateAdminUserResponse>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        users.MapPut("/{id:guid}", async (Guid id, UpdateAdminUserRequest request, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, http.GetLanguage(), ct)))
            .Produces<AdminUserDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        users.MapPost("/{id:guid}/disable", async (Guid id, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.DisableAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminUserDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        users.MapPost("/{id:guid}/enable", async (Guid id, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.EnableAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminUserDto>();
        users.MapPost("/{id:guid}/reset-password", async (Guid id, AdminUserService service, CancellationToken ct) =>
                Results.Ok(await service.ResetPasswordAsync(id, ct)))
            .Produces<TemporaryPasswordResponse>();
        users.MapPost("/{id:guid}/reset-mfa", async (Guid id, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ResetMfaAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminUserDto>();
        users.MapPost("/{id:guid}/unlock", async (Guid id, AdminUserService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.UnlockAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminUserDto>();
        users.MapGet("/{id:guid}/sessions", async (Guid id, AdminUserService service, CancellationToken ct) => Results.Ok(await service.SessionsAsync(id, ct)))
            .Produces<List<AdminSessionDto>>();
        users.MapDelete("/{id:guid}/sessions/{sessionId:guid}", async (Guid id, Guid sessionId, AdminUserService service, CancellationToken ct) =>
            {
                await service.RevokeSessionAsync(id, sessionId, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        users.MapPost("/{id:guid}/revoke-sessions", async (Guid id, AdminUserService service, CancellationToken ct) =>
            {
                await service.RevokeSessionsAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // ----- roles & permissions (admin.roles.manage) -----
        admin.MapGet("/permissions", async (RoleService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.PermissionsAsync(http.GetLanguage(), ct)))
            .RequirePermission(Permissions.AdminRolesManage)
            .Produces<List<PermissionDto>>();

        var roles = admin.MapGroup("/roles").RequirePermission(Permissions.AdminRolesManage);
        roles.MapGet("/", async (RoleService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.ListAsync(http.GetLanguage(), ct)))
            .Produces<List<RoleDto>>();
        roles.MapGet("/{id:guid}", async (Guid id, RoleService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .Produces<RoleDto>();
        roles.MapPost("/", async (RoleUpsertRequest request, RoleService service, HttpContext http, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/admin/roles/{created.Id}", created);
            })
            .Produces<RoleDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        roles.MapPut("/{id:guid}", async (Guid id, RoleUpsertRequest request, RoleService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, http.GetLanguage(), ct)))
            .Produces<RoleDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        roles.MapDelete("/{id:guid}", async (Guid id, RoleService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// <c>AdminSessionCleanupJob</c> (doc 12 §F20.8, hourly): revokes admin sessions that can no longer be refreshed — idle longer than
/// <c>Admin:SessionIdleMinutes</c> or past <c>absolute_expires_at</c>. Off with <c>Admin:JobsEnabled=false</c> (tests call <see cref="RunOnceAsync"/>).
/// </summary>
public sealed class AdminSessionCleanupJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<AdminOptions> options, ILogger<AdminSessionCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Admin session cleanup failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:admin_session_cleanup", TimeSpan.FromMinutes(10), ct);
        if (handle is null)
        {
            return 0;
        }

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtaDbContext>();
        var sessions = scope.ServiceProvider.GetRequiredService<AdminSessionIssuer>();
        var now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        var open = await db.RefreshTokens.Where(t => t.SessionKind == SessionKind.Admin && t.RevokedAt == null).ToListAsync(ct);
        var dead = open.Where(t => !sessions.IsAlive(t, now)).ToList();
        foreach (var token in dead)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return dead.Count;
    }
}
