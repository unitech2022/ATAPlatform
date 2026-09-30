using System.Text.RegularExpressions;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Infrastructure;

/// <summary>
/// F20: admin accounts for tests get their permissions through roles — <c>*</c> → the seeded <c>super_admin</c> role, anything else → a role
/// <c>test_{username}</c> holding exactly those catalogue permissions (an empty list → a role without permissions).
/// </summary>
public static partial class TestAdmins
{
    public static async Task EnsureAsync(ApiFixture fixture, string username, string password, IReadOnlyCollection<string> permissions)
    {
        await fixture.Factory.WithDbAsync(async db =>
        {
            if (await db.AdminAccounts.AnyAsync(a => a.Username == username))
            {
                return true;
            }

            using var scope = fixture.Factory.Services.CreateScope();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var user = new User { PhoneNumber = $"+9665{Random.Shared.Next(10000000, 99999999)}", FullName = $"Admin {username}", PhoneVerifiedAt = DateTime.UtcNow };
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Admin });
            db.Users.Add(user);
            var account = new AdminAccount { UserId = user.Id, Username = username, PasswordHash = hasher.Hash(password) };
            db.AdminAccounts.Add(account);
            Guid roleId;
            if (permissions.Contains(PermissionCatalog.All))
            {
                roleId = await db.Roles.Where(r => r.Code == PermissionCatalog.SuperAdminRole).Select(r => r.Id).FirstAsync();
            }
            else
            {
                var role = new AdminRole { Code = "test_" + NonCode().Replace(username.ToLowerInvariant(), "_"), NameAr = username, NameEn = username };
                db.Roles.Add(role);
                roleId = role.Id;
                var ids = await db.Permissions.Where(p => permissions.Contains(p.Code)).Select(p => p.Id).ToListAsync();
                foreach (var id in ids)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = id });
                }
            }

            db.AdminAccountRoles.Add(new AdminAccountRole { AdminAccountId = account.Id, RoleId = roleId });
            await db.SaveChangesAsync();
            return true;
        });
    }

    public static async Task<HttpClient> LoginWithAsync(ApiFixture fixture, string username, string password, params string[] permissions)
    {
        await EnsureAsync(fixture, username, password, permissions);
        return await fixture.LoginAdminAsync(username, password);
    }

    [GeneratedRegex("[^a-z0-9_]")]
    private static partial Regex NonCode();
}
