using DotNetEnv;
using ESDEMO.Api.Health;
using ESDEMO.Api.Middleware;
using ESDEMO.Application;
using ESDEMO.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var localEnvFile = LocalEnvironment.FindFile(builder.Environment.ContentRootPath);

    if (localEnvFile is not null)
    {
        var localConfiguration = Env.NoEnvVars()
            .Load(localEnvFile)
            .ToDictionary(
                pair => pair.Key.Replace("__", ":", StringComparison.Ordinal),
                pair => (string?)pair.Value,
                StringComparer.OrdinalIgnoreCase);

        builder.Configuration.AddInMemoryCollection(localConfiguration);
    }

    builder.Configuration.AddEnvironmentVariables();
}

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddHealthChecks()
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapControllers();

app.Run();

public partial class Program;

internal static class LocalEnvironment
{
    public static string? FindFile(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
