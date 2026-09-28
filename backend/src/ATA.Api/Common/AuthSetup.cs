using ATA.Domain.Common;
using ATA.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ATA.Api.Common;

public static class Policies
{
    public const string Authenticated = "Authenticated";
    public const string Passenger = "Passenger";
    public const string Driver = "Driver";
    public const string Admin = "Admin";
}

public static class AuthSetup
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddAtaAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = configuration[$"{JwtOptions.Section}:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through DI so that token lifetime is checked against the application clock (IClock).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, IClock>((options, jwtOptions, clock) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateKey(jwt.Key),
                    ValidateLifetime = true,
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = clock.UtcNow;
                        return (notBefore is null || notBefore <= now + ClockSkew) && (expires is null || expires > now - ClockSkew);
                    },
                    NameClaimType = AtaClaims.Subject,
                    RoleClaimType = AtaClaims.Roles,
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await context.HttpContext.WriteErrorAsync(ErrorCodes.Unauthorized, context.HttpContext.GetLanguage());
                    },
                    OnForbidden = context => context.HttpContext.WriteErrorAsync(ErrorCodes.Forbidden, context.HttpContext.GetLanguage()),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Authenticated, p => p.RequireAuthenticatedUser())
            .AddPolicy(Policies.Passenger, p => p.RequireRole(RoleNames.Passenger))
            .AddPolicy(Policies.Driver, p => p.RequireRole(RoleNames.Driver))
            .AddPolicy(Policies.Admin, p => p.RequireRole(RoleNames.Admin, RoleNames.Operations));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }
}
