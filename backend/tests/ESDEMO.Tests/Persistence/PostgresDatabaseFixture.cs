using ESDEMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ESDEMO.Tests.Persistence;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresDatabaseFixture.IsEnabled)
        {
            Skip = "Set ESDEMO_RUN_DATABASE_TESTS=1 and test PostgreSQL settings; see docs/database-model.md.";
        }
    }
}

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"esdemo_model_tests_{Guid.NewGuid():N}";
    private string? _adminConnectionString;
    private string? _testConnectionString;
    private bool _created;

    public static bool IsEnabled => Environment.GetEnvironmentVariable("ESDEMO_RUN_DATABASE_TESTS") == "1";

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("ESDEMO_TEST_POSTGRES_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("ESDEMO_TEST_POSTGRES_PORT") ?? "5432",
                System.Globalization.CultureInfo.InvariantCulture),
            Username = Environment.GetEnvironmentVariable("ESDEMO_TEST_POSTGRES_USER") ?? "esdemo",
            Password = Environment.GetEnvironmentVariable("ESDEMO_TEST_POSTGRES_PASSWORD")
                ?? throw new InvalidOperationException("A test PostgreSQL password must be provided."),
            Database = "postgres",
            Pooling = false
        };
        _adminConnectionString = builder.ConnectionString;

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        // Identifier contains only a fixed prefix and generated hexadecimal characters.
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        _created = true;
        builder.Database = _databaseName;
        _testConnectionString = builder.ConnectionString;

        try
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_testConnectionString ?? throw new InvalidOperationException("Test database is not initialized."))
            .Options);

    public string ConnectionString =>
        _testConnectionString ?? throw new InvalidOperationException("Test database is not initialized.");

    public async Task DisposeAsync()
    {
        if (!_created)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{_databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
        _created = false;
    }
}
