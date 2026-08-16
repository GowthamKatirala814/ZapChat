using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ZapChat.Shared.Auth;

public static class AuthSetup
{
    /// <summary>
    /// The one JWT configuration every ZapChat service uses.
    ///
    /// Three things here were previously wrong or missing per service:
    ///
    ///  1. The access token is read from the Authorization header, from the
    ///     access_token cookie, AND from ?access_token= on hub paths. Previously
    ///     only Auth.API read the cookie, which is why the frontend needed an
    ///     endpoint that echoed the HttpOnly cookie back as plaintext.
    ///  2. Authorization is DENY BY DEFAULT via a fallback policy. Any endpoint
    ///     without an explicit [AllowAnonymous] requires an authenticated user,
    ///     so a controller cannot be left open by omission.
    ///  3. ClockSkew is tightened from the 5-minute default to 30 seconds.
    /// </summary>
    public static IServiceCollection AddZapChatAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        params string[] hubPaths)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException(
                      "The 'Jwt' configuration section is missing. Set Jwt:Secret, Jwt:Issuer and " +
                      "Jwt:Audience via user-secrets or environment variables.");

        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret is missing or shorter than 32 characters. Provide it via " +
                "'dotnet user-secrets set Jwt:Secret <value>' or the ZAPCHAT_JWT__SECRET " +
                "environment variable. There is no built-in default.");
        }

        var accessCookie = jwt.AccessCookieName;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrEmpty(context.Token))
                            return Task.CompletedTask;

                        var path = context.HttpContext.Request.Path;

                        // SignalR WebSocket handshakes cannot set headers, so the
                        // client passes the token as a query parameter. Only honoured
                        // on declared hub paths.
                        if (hubPaths.Length > 0)
                        {
                            var queryToken = context.Request.Query["access_token"].ToString();
                            if (!string.IsNullOrEmpty(queryToken) &&
                                hubPaths.Any(h => path.StartsWithSegments(h)))
                            {
                                context.Token = queryToken;
                                return Task.CompletedTask;
                            }
                        }

                        // HttpOnly cookie — this is what lets the frontend stop
                        // fetching the raw JWT from an echo endpoint.
                        if (context.Request.Cookies.TryGetValue(accessCookie, out var cookieToken)
                            && !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ZapChatPolicies.AdminOnly,
                policy => policy.RequireAuthenticatedUser().RequireRole(ZapChatRoles.Admin));

            // Deny by default. This is the change that closes the whole class of
            // "controller has no [Authorize]" findings — an endpoint must now opt
            // out explicitly with [AllowAnonymous].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
