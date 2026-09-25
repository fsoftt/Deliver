namespace Deliver.Messaging;

/// <summary>
/// A public, versioned contract a service publishes to the rest of the system.
/// Each event declares where it is published so producers and consumers agree on routing.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Topic exchange owned by the producing service, e.g. <c>shipment.events</c>.</summary>
    static abstract string Exchange { get; }

    /// <summary>Routing key, e.g. <c>shipment.created</c>.</summary>
    static abstract string RoutingKey { get; }
}

public static class IntegrationEventNames
{
    /// <summary><c>ShipmentCreatedIntegrationEvent</c> becomes <c>ShipmentCreated</c> in the envelope.</summary>
    public static string EventTypeOf(Type eventType)
    {
        const string suffix = "IntegrationEvent";
        var name = eventType.Name;
        return name.EndsWith(suffix, StringComparison.Ordinal) ? name[..^suffix.Length] : name;
    }
}
