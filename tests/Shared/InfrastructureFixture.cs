using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Deliver.Testing;

/// <summary>
/// Real PostgreSQL and RabbitMQ in throw-away containers, shared by all tests of an assembly.
/// Integration tests run against the same kind of infrastructure as production: no in-memory fakes
/// that would hide transaction, locking or broker behaviour.
/// </summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder("rabbitmq:4-management-alpine").Build();

    public string RabbitMqConnectionString => RabbitMq.GetConnectionString();

    /// <summary>Each service gets its own database, exactly like in docker compose.</summary>
    public string DatabaseConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString()) { Database = database }.ToString();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(Postgres.StartAsync(), RabbitMq.StartAsync());

    public async ValueTask DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }
}
