using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Deliver.Testing;

/// <summary>Hosts a service in-process, wired to the test containers, with fast polling and retries.</summary>
public class ServiceFactory<TProgram>(InfrastructureFixture infrastructure, string connectionName, string database)
    : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = infrastructure.DatabaseConnectionString(database),
            ["ConnectionStrings:RabbitMq"] = infrastructure.RabbitMqConnectionString,
            ["Messaging:OutboxPollingInterval"] = "00:00:00.200",
            ["Messaging:RetryDelays:0"] = "00:00:00.500",
            ["Messaging:RetryDelays:1"] = "00:00:01",
        }));
    }
}
