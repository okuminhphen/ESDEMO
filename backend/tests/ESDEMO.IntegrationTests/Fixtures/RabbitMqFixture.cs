using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace ESDEMO.Tests.Integration.Fixtures;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.3.5-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public ConnectionFactory CreateConnectionFactory() => new()
    {
        Uri = new Uri(ConnectionString),
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
}
