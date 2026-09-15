using ESDEMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ESDEMO.Tests.Integration.Fixtures;

public sealed class PostgresFactAttribute : FactAttribute { }

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("esdemo_tests")
        .WithUsername("esdemo")
        .WithPassword("testcontainers-local-only")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}





