using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Corporate;
using ATA.Domain.Identity;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F19 membership: invitations (SMS + in-app), acceptance by phone match, expiry, one company per user, disabling, portal OTP login and company isolation.</summary>
public class CorporateMembershipTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    [Fact]
    public async Task Invitation_is_sent_by_sms_and_in_app_and_accepted_in_the_rider_app()
    {
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة الدعوات");
        var phone = fixture.NextPhone();
        var (employee, auth) = await fixture.LoginAsync("passenger", phone);
        (await employee.PatchAsJsonAsync("/api/v1/me", new { fullName = "منى" })).EnsureSuccessStatusCode();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await (await employee.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).ValueKind);

        var invited = await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new
        {
            phoneNumber = phone, fullName = "منى", role = "employee", employeeNumber = "E-77", department = "المالية", monthlyBudget = 900m,
        });
        Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
        var dto = await invited.ReadJsonAsync();
        Assert.Equal("invited", dto.GetProperty("status").GetString());
        Assert.Equal("E-77", dto.GetProperty("employeeNumber").GetString());
        Assert.Equal(900m, dto.GetProperty("monthlyBudget").GetDecimal());
        Assert.Equal(0m, dto.GetProperty("spentThisMonth").GetDecimal());
        var memberId = dto.GetProperty("id").GetString();

        // SMS (with the portal link) + inbox/push rows for the existing user.
        await fixture.Factory.RunNotificationWorkerAsync();
        var sms = fixture.Sms.Sent.Where(m => m.Phone == CorporateFlow.Normalize(phone)).ToList();
        Assert.Single(sms);
        Assert.Contains("https://ata.test/business/join/", sms[0].Message);
        Assert.Contains("شركة الدعوات", sms[0].Message);
        var inbox = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().Where(n => n.UserId == PaymentFlow.UserId(auth) && n.Type == "corporate.invitation").ToListAsync());
        Assert.Single(inbox);
        Assert.Contains("ata://corporate/invitations", inbox[0].Data);

        // The token is only stored hashed.
        var stored = await fixture.Factory.WithDbAsync(db => db.CorporateInvitations.AsNoTracking().SingleAsync(i => i.CorporateUserId == Guid.Parse(memberId!)));
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.DoesNotContain(stored.TokenHash, sms[0].Message);
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddDays(7), stored.ExpiresAt);

        var invitations = await (await employee.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        Assert.Equal(1, invitations.GetArrayLength());
        Assert.Equal("شركة الدعوات", invitations[0].GetProperty("companyName").GetString());
        Assert.Equal("employee", invitations[0].GetProperty("role").GetString());
        var invitationId = invitations[0].GetProperty("id").GetString();

        // Another rider cannot answer it (matching is by the verified phone number).
        var (stranger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/api/v1/passenger/corporate/invitations/{invitationId}/accept", null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await employee.PostAsync($"/api/v1/passenger/corporate/invitations/{invitationId}/accept", null)).StatusCode);
        var membership = await (await employee.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync();
        Assert.Equal("active", membership.GetProperty("membership").GetProperty("status").GetString());
        Assert.Equal("شركة الدعوات", membership.GetProperty("membership").GetProperty("companyName").GetString());
        Assert.Equal("E-77", membership.GetProperty("membership").GetProperty("employeeNumber").GetString());
        Assert.Equal(900m, membership.GetProperty("budget").GetProperty("monthly").GetDecimal());
        Assert.Equal(0m, membership.GetProperty("budget").GetProperty("spent").GetDecimal());
        Assert.Equal(900m, membership.GetProperty("budget").GetProperty("remaining").GetDecimal());
        Assert.Equal(0, (await (await employee.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync()).GetArrayLength());

        var member = await fixture.Factory.WithDbAsync(db => db.CorporateUsers.AsNoTracking().SingleAsync(m => m.Id == Guid.Parse(memberId!)));
        Assert.Equal(CorporateUserStatus.Active, member.Status);
        Assert.Equal(PaymentFlow.UserId(auth), member.UserId);
        Assert.NotNull(member.ActivatedAt);
        var row = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.Action == "corporate_user.accept" && a.ActorUserId == PaymentFlow.UserId(auth)).SingleAsync());
        Assert.Null(row.ActorRole == "corporate_admin" ? "" : null);

        // Accepting twice is a conflict; the roster shows the employee as active.
        Assert.Equal(HttpStatusCode.Conflict, (await employee.PostAsync($"/api/v1/passenger/corporate/invitations/{invitationId}/accept", null)).StatusCode);
        var roster = await (await company.Portal.GetAsync($"/api/v1/corporate/employees/{memberId}")).ReadJsonAsync();
        Assert.Equal("active", roster.GetProperty("status").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, roster.GetProperty("invitationExpiresAt").ValueKind);
    }

    [Fact]
    public async Task Decline_resend_and_cancel_an_invitation()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        var declining = await CorporateFlow.AddEmployeeAsync(fixture, company, "رافض", accept: false);
        var invitations = await (await declining.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        var id = invitations[0].GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.OK, (await declining.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/decline", null)).StatusCode);
        Assert.Equal(0, (await (await declining.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await declining.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/accept", null)).StatusCode);

        // Resending revokes the old invitation and issues a new one that can be accepted.
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.PostAsync($"/api/v1/corporate/employees/{declining.MemberId}/resend-invitation", null)).StatusCode);
        var fresh = await (await declining.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        Assert.Equal(1, fresh.GetArrayLength());
        Assert.NotEqual(id, fresh[0].GetProperty("id").GetString());
        await CorporateFlow.AcceptAsync(declining.Client);
        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.PostAsync($"/api/v1/corporate/employees/{declining.MemberId}/resend-invitation", null)).StatusCode);

        // DELETE cancels the invitation of someone who never accepted, not of an active member.
        var pending = await CorporateFlow.AddEmployeeAsync(fixture, company, "معلّق", accept: false);
        Assert.Equal(HttpStatusCode.NoContent, (await company.Portal.DeleteAsync($"/api/v1/corporate/employees/{pending.MemberId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.GetAsync($"/api/v1/corporate/employees/{pending.MemberId}")).StatusCode);
        Assert.Equal(0, (await (await pending.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.DeleteAsync($"/api/v1/corporate/employees/{declining.MemberId}")).StatusCode);

        var duplicate = await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = "0" + declining.Phone[4..], fullName = "مكرر", role = "employee" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = "123", fullName = "x", role = "employee" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = fixture.NextPhone(), fullName = "x", role = "manager" })).StatusCode);
    }

    [Fact]
    public async Task Expired_invitation_answers_410_and_the_expiry_job_marks_it_once()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "متأخر", accept: false);
        var invitations = await (await employee.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        var id = invitations[0].GetProperty("id").GetString();

        fixture.Factory.Clock.Advance(TimeSpan.FromDays(8));
        var (client, _) = await fixture.LoginAsync("passenger", "0" + employee.Phone[4..]);
        employee = employee with { Client = client };
        Assert.Equal(0, (await (await employee.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync()).GetArrayLength());
        var expired = await employee.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/accept", null);
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        Assert.Equal("invitation_expired", await expired.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Gone, (await employee.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/decline", null)).StatusCode);

        Assert.True(await fixture.Factory.RunCorporateInvitationExpiryAsync() >= 1);
        Assert.Equal(0, await fixture.Factory.RunCorporateInvitationExpiryAsync());
        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.Action == "corporate_invitation.expire").ToListAsync());
        Assert.All(audit, a => Assert.Equal("system", a.ActorRole));
        Assert.Equal(HttpStatusCode.Gone, (await employee.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/accept", null)).StatusCode);

        // The company re-invites with a fresh link (its admin signs in again: the old access token lapsed with the clock).
        var (portal, _) = await fixture.LoginAsync("corporate_admin", company.AdminPhone);
        Assert.Equal(HttpStatusCode.OK, (await portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/resend-invitation", null)).StatusCode);
        await CorporateFlow.AcceptAsync(employee.Client);
        Assert.Equal("active", (await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).GetProperty("membership").GetProperty("status").GetString());
    }

    [Fact]
    public async Task A_user_is_an_active_member_of_one_company_only()
    {
        var first = await CorporateFlow.OnboardAsync(fixture, "الأولى");
        var second = await CorporateFlow.OnboardAsync(fixture, "الثانية");
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, first, "موظف مشترك");
        var phone = "0" + employee.Phone[4..];

        // The second company may invite the same number, but the acceptance is refused.
        var invited = await second.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = phone, fullName = "موظف مشترك", role = "employee" });
        Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
        var invitations = await (await employee.Client.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        var conflict = await employee.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{invitations[0].GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("corporate_member_elsewhere", await conflict.ErrorCodeAsync());
        Assert.Equal("الأولى", (await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).GetProperty("membership").GetProperty("companyName").GetString());

        // Once the first company disables the employee the second invitation can be accepted.
        Assert.Equal(HttpStatusCode.OK, (await first.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/disable", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await employee.Client.PostAsync($"/api/v1/passenger/corporate/invitations/{invitations[0].GetProperty("id").GetString()}/accept", null)).StatusCode);
        Assert.Equal("الثانية", (await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).GetProperty("membership").GetProperty("companyName").GetString());
        // …and the first company cannot re-enable the member who now belongs to another company.
        var enable = await first.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/enable", null);
        Assert.Equal(HttpStatusCode.Conflict, enable.StatusCode);
        Assert.Equal("corporate_member_elsewhere", await enable.ErrorCodeAsync());
    }

    [Fact]
    public async Task Disable_enable_update_and_the_last_admin_rule()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف");
        var costCenter = await CorporateFlow.CreateCostCenterAsync(company, "OPS-1", "العمليات");
        var updated = await company.Portal.PutAsJsonAsync($"/api/v1/corporate/employees/{employee.MemberId}", new
        {
            phoneNumber = employee.Phone, fullName = "موظف معدّل", role = "employee", employeeNumber = "E-9", department = "العمليات", costCenterId = costCenter.GetProperty("id").GetString(), monthlyBudget = 300m,
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var dto = await updated.ReadJsonAsync();
        Assert.Equal("موظف معدّل", dto.GetProperty("fullName").GetString());
        Assert.Equal("OPS-1", dto.GetProperty("costCenter").GetString());
        Assert.Equal(300m, dto.GetProperty("monthlyBudget").GetDecimal());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PutAsJsonAsync($"/api/v1/corporate/employees/{employee.MemberId}", new { fullName = "x", role = "employee", costCenterId = Guid.NewGuid() })).StatusCode);

        // Disabling blocks booking at once (and the rider app sees the disabled membership).
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/disable", null)).StatusCode);
        Assert.Equal("disabled", (await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).GetProperty("membership").GetProperty("status").GetString());
        var blocked = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(TripFlow.Area(0)));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("corporate_not_member", await blocked.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/disable", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/enable", null)).StatusCode);
        Assert.Equal("active", (await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync()).GetProperty("membership").GetProperty("status").GetString());

        // The last active admin can neither be disabled nor demoted.
        var me = await fixture.Factory.WithDbAsync(db => db.CorporateUsers.AsNoTracking().SingleAsync(m => m.UserId == company.AdminUserId));
        var disable = await company.Portal.PostAsync($"/api/v1/corporate/employees/{me.Id}/disable", null);
        Assert.Equal(HttpStatusCode.Conflict, disable.StatusCode);
        Assert.Equal("last_admin", (await disable.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var demote = await company.Portal.PutAsJsonAsync($"/api/v1/corporate/employees/{me.Id}", new { fullName = "المسؤول", role = "employee" });
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);

        // With a second admin the first can step down; the actions are audited with the actor's role.
        var second = await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = fixture.NextPhone(), fullName = "مسؤول ثان", role = "corporate_admin" });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.ActorRole == "corporate_admin").Select(a => a.Action).Distinct().ToListAsync());
        Assert.Contains("corporate_user.invite", actions);
        Assert.Contains("corporate_user.update", actions);
        Assert.Contains("corporate_user.disable", actions);
        Assert.Contains("corporate_user.enable", actions);
    }

    [Fact]
    public async Task Portal_login_requires_a_corporate_admin_membership_and_scopes_the_token_to_the_company()
    {
        // OTP is always sent for role corporate_admin (membership is not revealed), but verifying needs a membership.
        var stranger = fixture.NextPhone();
        using var anonymous = fixture.CreateClient();
        var requested = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = stranger, role = "corporate_admin", language = "ar" });
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        var otp = await requested.ReadJsonAsync();
        var denied = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId = otp.GetProperty("requestId").GetString(), phoneNumber = stranger, code = otp.GetProperty("devCode").GetString(), role = "corporate_admin" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("corporate_not_member", await denied.ErrorCodeAsync());
        Assert.False(await fixture.Factory.WithDbAsync(db => db.Users.AnyAsync(u => u.PhoneNumber == CorporateFlow.Normalize(stranger))), "no user is created for a non-member");

        // An employee (not an admin) of a company is not a member for the portal.
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة البوابة");
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف");
        await AssertLoginDeniedAsync(employee.Phone, "corporate_not_member");

        // The admin gets a corporate token: role corporate_admin, the corp claim, a corporate refresh session.
        Assert.Contains("corporate_admin", company.PortalAuth.GetProperty("user").GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        var claims = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ReadJsonWebToken(company.PortalAuth.GetProperty("accessToken").GetString()!).Claims.ToList();
        Assert.Equal(company.AccountId.ToString(), claims.Single(c => c.Type == "corp").Value);
        Assert.Equal(["corporate_admin"], claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList());
        var session = await fixture.Factory.WithDbAsync(db => db.RefreshTokens.AsNoTracking().Where(t => t.UserId == company.AdminUserId).OrderByDescending(t => t.CreatedAt).FirstAsync());
        Assert.Equal(SessionKind.Corporate, session.SessionKind);

        // The token only works on /corporate/*: not as rider, driver or admin.
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.GetAsync("/api/v1/corporate/account")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await company.Portal.GetAsync("/api/v1/passenger/trips")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await company.Portal.GetAsync("/api/v1/driver/trips")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await company.Portal.GetAsync("/api/v1/admin/drivers")).StatusCode);
        // A rider token is refused by the portal.
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.Client.GetAsync("/api/v1/corporate/account")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/api/v1/corporate/account")).StatusCode);

        // Refreshing keeps the corporate session (same corp claim) while the admin stays an active member.
        using var anonymous2 = fixture.CreateClient();
        var refreshed = await anonymous2.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = company.PortalAuth.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var refreshedClaims = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().ReadJsonWebToken((await refreshed.ReadJsonAsync()).GetProperty("accessToken").GetString()!).Claims.ToList();
        Assert.Equal(company.AccountId.ToString(), refreshedClaims.Single(c => c.Type == "corp").Value);
    }

    [Fact]
    public async Task A_company_never_sees_or_touches_the_data_of_another_company()
    {
        var first = await CorporateFlow.OnboardAsync(fixture, "شركة أ");
        var second = await CorporateFlow.OnboardAsync(fixture, "شركة ب");
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, first, "موظف أ");
        var policy = await CorporateFlow.CreatePolicyAsync(first, new { name = "سياسة أ" });
        var center = await CorporateFlow.CreateCostCenterAsync(first, "A-1", "مركز أ");

        Assert.Equal(HttpStatusCode.NotFound, (await second.Portal.GetAsync($"/api/v1/corporate/employees/{employee.MemberId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.Portal.PutAsJsonAsync($"/api/v1/corporate/employees/{employee.MemberId}", new { fullName = "x", role = "employee" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.Portal.PostAsync($"/api/v1/corporate/employees/{employee.MemberId}/disable", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.Portal.PutAsJsonAsync($"/api/v1/corporate/policies/{policy.GetProperty("id").GetString()}", new { name = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await second.Portal.DeleteAsync($"/api/v1/corporate/cost-centers/{center.GetProperty("id").GetString()}")).StatusCode);
        Assert.Equal(0, (await (await second.Portal.GetAsync("/api/v1/corporate/employees")).ReadJsonAsync()).GetProperty("items").GetArrayLength() - 1);
        Assert.Equal(0, (await (await second.Portal.GetAsync("/api/v1/corporate/policies")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(0, (await (await second.Portal.GetAsync("/api/v1/corporate/cost-centers")).ReadJsonAsync()).GetArrayLength());
        // A cost centre / policy of another company cannot be assigned.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await second.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new
        {
            phoneNumber = fixture.NextPhone(), fullName = "x", role = "employee", costCenterId = center.GetProperty("id").GetString(),
        })).StatusCode);
    }

    private async Task AssertLoginDeniedAsync(string phone, string code)
    {
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(2)); // the OTP resend window of a number that just signed in
        using var anonymous = fixture.CreateClient();
        var requested = await (await anonymous.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "corporate_admin", language = "ar" })).ReadJsonAsync();
        var verify = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId = requested.GetProperty("requestId").GetString(), phoneNumber = phone, code = requested.GetProperty("devCode").GetString(), role = "corporate_admin" });
        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);
        Assert.Equal(code, await verify.ErrorCodeAsync());
    }

    [Fact]
    public async Task Closed_company_blocks_the_portal_and_an_expired_admin_invitation_answers_410()
    {
        using var platform = await fixture.LoginAdminAsync();
        var account = await CorporateFlow.CreateAccountAsync(platform);
        var id = account.GetProperty("id").GetString();
        var phone = fixture.NextPhone();
        await platform.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/admins", new { phoneNumber = phone, fullName = "مسؤول" });
        fixture.Factory.Clock.Advance(TimeSpan.FromDays(9));
        await AssertLoginDeniedWith410Async(phone);

        // A fresh invitation works; closing the company then blocks the admin with corporate_account_inactive.
        using var admin = await fixture.LoginAdminAsync();
        var second = await CorporateFlow.CreateAccountAsync(admin);
        var secondId = second.GetProperty("id").GetString();
        var secondPhone = fixture.NextPhone();
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{secondId}/admins", new { phoneNumber = secondPhone, fullName = "مسؤول" });
        var (portal, _) = await fixture.LoginAsync("corporate_admin", secondPhone);
        Assert.Equal(HttpStatusCode.OK, (await portal.GetAsync("/api/v1/corporate/account")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{secondId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{secondId}/close", new { reason = "إنهاء" })).StatusCode);
        var inactive = await portal.GetAsync("/api/v1/corporate/account");
        Assert.Equal(HttpStatusCode.Forbidden, inactive.StatusCode);
        Assert.Equal("corporate_account_inactive", await inactive.ErrorCodeAsync());
        await AssertLoginDeniedAsync(secondPhone, "corporate_account_inactive");
    }

    private async Task AssertLoginDeniedWith410Async(string phone)
    {
        using var anonymous = fixture.CreateClient();
        var requested = await (await anonymous.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "corporate_admin", language = "ar" })).ReadJsonAsync();
        var verify = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId = requested.GetProperty("requestId").GetString(), phoneNumber = phone, code = requested.GetProperty("devCode").GetString(), role = "corporate_admin" });
        Assert.Equal(HttpStatusCode.Gone, verify.StatusCode);
        Assert.Equal("invitation_expired", await verify.ErrorCodeAsync());
    }
}
