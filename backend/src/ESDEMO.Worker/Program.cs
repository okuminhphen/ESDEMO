using DotNetEnv;
using ESDEMO.Infrastructure;
using ESDEMO.Worker.Options;
using ESDEMO.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
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
// Deployment environment variables are intentionally added last and override any local .env value.
builder.Configuration.AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Services.AddWorkerInfrastructure(builder.Configuration);
builder.Services.AddOptions<OutboxWorkerOptions>()
    .Bind(builder.Configuration.GetSection(OutboxWorkerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<PurchaseNotificationConsumerWorker>();

await builder.Build().RunAsync();

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

