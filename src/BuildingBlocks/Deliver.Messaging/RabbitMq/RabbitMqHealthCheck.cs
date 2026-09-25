using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Deliver.Messaging.RabbitMq;

internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(connection.IsOpen
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("RabbitMQ connection is not open."));
}
