using Deliver.Messaging;

namespace Deliver.Contracts.Fleet;

public static class FleetExchange
{
    public const string Name = "fleet.events";
}

/// <summary>A driver can take new deliveries. Carries what dispatchers need to match work to drivers.</summary>
public sealed record DriverAvailableIntegrationEvent(
    Guid DriverId,
    string Name,
    decimal VehicleCapacityKg,
    DateTimeOffset AvailableSince) : IIntegrationEvent
{
    public static string Exchange => FleetExchange.Name;
    public static string RoutingKey => "driver.available";
}

public sealed record DriverUnavailableIntegrationEvent(
    Guid DriverId,
    string Reason,
    DateTimeOffset UnavailableSince) : IIntegrationEvent
{
    public static string Exchange => FleetExchange.Name;
    public static string RoutingKey => "driver.unavailable";
}

/// <summary>Fleet could not honour an assignment (e.g. the driver went off shift concurrently).</summary>
public sealed record DriverAssignmentRejectedIntegrationEvent(
    Guid ShipmentId,
    Guid DriverId,
    string Reason,
    DateTimeOffset RejectedAt) : IIntegrationEvent
{
    public static string Exchange => FleetExchange.Name;
    public static string RoutingKey => "driver.assignment_rejected";
}
