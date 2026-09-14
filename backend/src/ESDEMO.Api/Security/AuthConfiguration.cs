using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ESDEMO.Application.Auth.Abstractions;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ESDEMO.Api.Security;

public static class AuthConfiguration
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.SaveToken = false;
                bearer.IncludeErrorDetails = false;
                bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters();
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var userId))
                        {
                            context.Fail("Invalid account.");
                            return;
                        }
                        var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        UserResponseDto user;
                        try
                        {
                            user = await auth.GetUserAsync(userId, context.HttpContext.RequestAborted);
                        }
                        catch (AuthenticationFailedException)
                        {
                            context.Fail("Invalid account.");
                            return;
                        }

                        // Authorization uses current database roles so demotion takes effect
                        // on the next authenticated request, even with an older valid JWT.
                        var identity = (ClaimsIdentity)context.Principal!.Identity!;
                        foreach (var claim in identity.FindAll("role").ToArray()) { identity.RemoveClaim(claim); }
                        identity.AddClaims(user.Roles.Select(role => new Claim("role", role)));
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await WriteProblemAsync(context.HttpContext, 401, "Authentication is required.");
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, 403, "Access is denied.")
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(ApplicationRoleNames.Admin, policy => policy.RequireAuthenticatedUser().RequireRole(ApplicationRoleNames.Admin))
            .AddPolicy(ApplicationRoleNames.Customer, policy => policy.RequireAuthenticatedUser().RequireRole(ApplicationRoleNames.Customer));

        services.AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection("AuthRateLimit")).ValidateDataAnnotations().ValidateOnStart();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<AuthRateLimitOptions>>((options, settings) =>
        {
            var limits = settings.Value;
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitOptions.PolicyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }
                await WriteProblemAsync(context.HttpContext, 429, "Too many requests. Try again later.");
            };
        });
        return services;
    }

    private static Task WriteProblemAsync(HttpContext context, int status, string title)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
