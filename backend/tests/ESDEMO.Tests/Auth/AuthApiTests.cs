using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using ESDEMO.Application.Auth.Commands.Register;
using ESDEMO.Application.Auth.Dtos;
using ESDEMO.Application.Common.Exceptions;
using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Identity;
using ESDEMO.Infrastructure.Persistence;
using ESDEMO.Tests.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ESDEMO.Tests.Auth;

public sealed class AuthApiTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private const string Password = "Valid_Test_Password!2026";

    [PostgresFact]
    public async Task Register_login_and_me_return_safe_dtos_and_only_customer_role()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = NewEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, displayName = " Customer ", password = Password, confirmPassword = Password, role = "Admin", roles = new[] { "Admin" } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResponseDto>())!;
        Assert.Equal(["Customer"], user.Roles);
        Assert.Equal("Customer", user.DisplayName);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        var login = await LoginAsync(client, email);
        Assert.Equal(user.Id, login.User.Id);
        Assert.True(login.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(login.RefreshTokenExpiresAt > login.AccessTokenExpiresAt);
        Authorize(client, login);
        var me = await client.GetFromJsonAsync<UserResponseDto>("/api/auth/me");
        Assert.Equal(user.Id, me!.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedUser = await db.Users.SingleAsync(item => item.Id == user.Id);
        Assert.NotEqual(Password, storedUser.PasswordHash);
        Assert.False(storedUser.EmailConfirmed);
        var session = await db.RefreshSessions.SingleAsync(item => item.UserId == user.Id);
        Assert.Equal(64, session.TokenHash.Length);
        Assert.NotEqual(login.RefreshToken, session.TokenHash);
    }

    [PostgresFact]
    public async Task Invalid_dtos_and_weak_passwords_return_validation_problem()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        foreach (var data in new[]
        {
            new RegisterRequestDto { Email = "bad", DisplayName = "A", Password = "x", ConfirmPassword = "y" },
            new RegisterRequestDto { Email = NewEmail(), DisplayName = "  ", Password = Password, ConfirmPassword = Password },
            new RegisterRequestDto { Email = NewEmail(), DisplayName = "Customer", Password = "alllowercasepassword", ConfirmPassword = "alllowercasepassword" },
            new RegisterRequestDto { Email = NewEmail(), DisplayName = "Customer", Password = Password, ConfirmPassword = "Different_Password!2026" }
        })
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", data);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
        }
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/login", new { email = "bad", password = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "bad" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "" })).StatusCode);
    }

    [PostgresFact]
    public async Task MediatR_validation_also_rejects_invalid_input_without_http()
    {
        await using var factory = await CreateFactoryAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        await Assert.ThrowsAsync<RequestValidationException>(() => scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new RegisterCommand(new RegisterRequestDto())));
    }

    [PostgresFact]
    public async Task Concurrent_registration_and_case_variants_do_not_create_duplicate_users()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = NewEmail();
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register", Registration(email)),
            client.PostAsJsonAsync("/api/auth/register", Registration(email.ToUpperInvariant())));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.CountAsync(user => user.NormalizedEmail == email.ToUpperInvariant()));
    }

    [PostgresFact]
    public async Task Unknown_wrong_password_and_locked_accounts_use_same_error_and_lockout_persists()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);
        var missing = await client.PostAsJsonAsync("/api/auth/login", new { email = NewEmail(), password = Password });
        var title = (await missing.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString();
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong_Password!2026" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal(title, (await wrong.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        }
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password })).StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.SingleAsync(user => user.Email == email);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow);
    }

    [PostgresFact]
    public async Task Refresh_rotates_and_reuse_revokes_family_but_not_another_login()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);
        var original = await LoginAsync(client, email);
        var separateLogin = await LoginAsync(client, email);
        var response = await RefreshAsync(client, original);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
        Assert.NotEqual(original.RefreshToken, rotated.RefreshToken);
        Assert.True((original.RefreshTokenExpiresAt - rotated.RefreshTokenExpiresAt).Duration() < TimeSpan.FromMilliseconds(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, original)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, rotated)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(client, separateLogin)).StatusCode);
    }

    [PostgresFact]
    public async Task Concurrent_refresh_has_one_winner_and_replay_revokes_its_replacement()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = await LoginAsync(client, await RegisterAsync(client));
        var responses = await Task.WhenAll(RefreshAsync(client, original), RefreshAsync(client, original));
        var success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
        var rotated = (await success.Content.ReadFromJsonAsync<TokenResponseDto>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, rotated)).StatusCode);
    }

    [PostgresFact]
    public async Task Logout_is_idempotent_and_revokes_family_even_with_a_rotated_token()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var original = await LoginAsync(client, await RegisterAsync(client));
        var rotated = (await (await RefreshAsync(client, original)).Content.ReadFromJsonAsync<TokenResponseDto>())!;
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PostAsJsonAsync("/api/auth/logout", new { original.RefreshToken })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, rotated)).StatusCode);
        // Logout revokes refresh tokens; already-issued JWTs live until expiry.
        Authorize(client, rotated);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [PostgresFact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, await RegisterAsync(client));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .RefreshSessions.Where(item => item.UserId == login.User.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.CreatedAt, DateTimeOffset.UtcNow.AddDays(-10))
                    .SetProperty(item => item.ExpiresAt, DateTimeOffset.UtcNow.AddDays(-1)));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, login)).StatusCode);
    }

    [PostgresFact]
    public async Task Role_policies_and_account_deactivation_use_current_database_state()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var login = await LoginAsync(client, await RegisterAsync(client));
        Authorize(client, login);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/test/authorization/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/test/authorization/customer")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            Assert.True((await manager.AddToRoleAsync((await manager.FindByIdAsync(login.User.Id.ToString()))!, ApplicationRoleNames.Admin)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/test/authorization/admin")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            Assert.True((await manager.RemoveFromRoleAsync((await manager.FindByIdAsync(login.User.Id.ToString()))!, ApplicationRoleNames.Admin)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/test/authorization/admin")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.Where(user => user.Id == login.User.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.IsActive, false));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(client, login)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/auth/login", new { login.User.Email, password = Password })).StatusCode);
    }

    [PostgresFact]
    public async Task Invalid_jwt_signature_issuer_audience_expiry_and_algorithm_are_rejected()
    {
        await using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var login = await LoginAsync(client, await RegisterAsync(client));
        var jwt = factory.Jwt;
        var now = DateTime.UtcNow;
        Claim[] claims = [new("sub", login.User.Id.ToString())];
        var signing = new SigningCredentials(jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha256);
        var variants = new[]
        {
            new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, now, now.AddMinutes(5),
                new SigningCredentials(new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)), SecurityAlgorithms.HmacSha256)),
            new JwtSecurityToken("wrong-issuer", jwt.Audience, claims, now, now.AddMinutes(5), signing),
            new JwtSecurityToken(jwt.Issuer, "wrong-audience", claims, now, now.AddMinutes(5), signing),
            new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, now.AddMinutes(-10), now.AddMinutes(-5), signing),
            new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, now, now.AddMinutes(5),
                new SigningCredentials(jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha512)),
            new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, now, now.AddMinutes(5)),
            new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, signingCredentials: signing)
        };
        foreach (var token in variants)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
            var response = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
        }
    }

    [PostgresFact]
    public async Task Rate_limit_returns_429_and_retry_after()
    {
        await using var factory = await CreateFactoryAsync(2);
        using var client = factory.CreateClient();
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsJsonAsync("/api/auth/login", new { email = "bad", password = "" })).StatusCode);
        }
        var limited = await client.PostAsJsonAsync("/api/auth/login", new { email = "bad", password = "" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    private async Task<AuthApiFactory> CreateFactoryAsync(int permitLimit = 1000)
    {
        var factory = new AuthApiFactory(database.ConnectionString, permitLimit);
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        return factory;
    }

    private static string NewEmail() => $"customer-{Guid.NewGuid():N}@example.test";
    private static RegisterRequestDto Registration(string email) => new()
    {
        Email = email,
        DisplayName = "Customer",
        Password = Password,
        ConfirmPassword = Password
    };
    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration(email))).StatusCode);
        return email;
    }

    private static async Task<TokenResponseDto> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Email = email, Password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await response.Content.ReadFromJsonAsync<TokenResponseDto>())!;
    }

    private static void Authorize(HttpClient client, TokenResponseDto tokens) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, TokenResponseDto tokens) =>
        client.PostAsJsonAsync("/api/auth/refresh", new { tokens.RefreshToken });
}

public sealed class AuthApiFactory(string connectionString, int permitLimit) : WebApplicationFactory<Program>
{
    public JwtOptions Jwt { get; } = new()
    {
        Issuer = "esdemo-tests",
        Audience = "esdemo-test-client",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
                ["RabbitMq:Host"] = "localhost",
                ["RabbitMq:Username"] = "test",
                ["RabbitMq:Password"] = "test",
                ["Jwt:Issuer"] = Jwt.Issuer,
                ["Jwt:Audience"] = Jwt.Audience,
                ["Jwt:SigningKey"] = Jwt.SigningKey,
                ["AuthRateLimit:PermitLimit"] = permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Logging:LogLevel:Default"] = "Warning",
                ["SeedAdmin:Enabled"] = "false"
            }));
        builder.ConfigureTestServices(services => services.AddControllers().AddApplicationPart(typeof(AuthPolicyProbeController).Assembly));
    }
}

// These endpoints are registered only by the test host, never the production API.
[ApiController]
[Route("test/authorization")]
public sealed class AuthPolicyProbeController : ControllerBase
{
    [HttpGet("admin"), Authorize(Policy = ApplicationRoleNames.Admin)]
    public IActionResult Admin() => Ok();

    [HttpGet("customer"), Authorize(Policy = ApplicationRoleNames.Customer)]
    public IActionResult Customer() => Ok();
}
