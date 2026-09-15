using System.Security.Cryptography;
using ESDEMO.Application.Common.Security;
using ESDEMO.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Tests.Integration.Fixtures;
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





