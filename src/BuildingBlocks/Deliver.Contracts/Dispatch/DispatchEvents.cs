using Deliver.Messaging;

namespace Deliver.Contracts.Dispatch;

public static class DispatchExchange
{
    public const string Name = "dispatch.events";
}

public sealed record DriverAssignedIntegrationEvent(
    Guid AssignmentId,
    Guid ShipmentId,
    Guid DriverId,
    string DriverName,
    DateTimeOffset AssignedAt) : IIntegrationEvent
{
    public static string Exchange => DispatchExchange.Name;
    public static string RoutingKey => "driver.assigned";
}
