using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Deliver.Contracts.Shipping;
using Deliver.Messaging;

namespace Deliver.Contracts.Tests;

/// <summary>
/// Integration events are public contracts between independently deployed services. These tests make
/// every change to them deliberate: renaming or removing a field breaks consumers that are still running
/// the old code, so the approved schema snapshot must be updated consciously (and reviewed).
/// </summary>
public class IntegrationEventContractTests
{
    private static readonly Type[] AllowedPropertyTypes =
    [
        typeof(Guid), typeof(Guid?), typeof(string), typeof(int), typeof(decimal),
        typeof(DateTimeOffset), typeof(AddressDto),
    ];

    private static readonly Dictionary<string, string> ExchangeOwnerByNamespace = new()
    {
        ["Deliver.Contracts.Shipping"] = "shipment.events",
        ["Deliver.Contracts.Fleet"] = "fleet.events",
        ["Deliver.Contracts.Dispatch"] = "dispatch.events",
        ["Deliver.Contracts.Billing"] = "billing.events",
    };

    private static readonly List<Type> Events = typeof(ShipmentCreatedIntegrationEvent).Assembly.GetTypes()
        .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(IIntegrationEvent)))
        .OrderBy(t => t.FullName, StringComparer.Ordinal)
        .ToList();

    [Fact]
    public void Every_event_is_an_immutable_sealed_record()
    {
        Events.ShouldNotBeEmpty();
        Events.ShouldAllBe(t => t.IsSealed && t.GetMethod("<Clone>$") != null);
        Events.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)).ShouldAllBe(p => p.SetMethod == null || p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Length > 0);
    }

    [Fact]
    public void Events_only_use_primitive_types_never_domain_types()
    {
        var offending = Events
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => (Event: t.Name, Property: p)))
            .Where(x => !AllowedPropertyTypes.Contains(x.Property.PropertyType))
            .Select(x => $"{x.Event}.{x.Property.Name}: {x.Property.PropertyType.Name}")
            .ToList();

        offending.ShouldBeEmpty();
    }

    [Fact]
    public void Each_event_is_published_on_its_owners_exchange_with_a_unique_routing_key()
    {
        var routes = Events.Select(t => (Type: t, Exchange: StaticString(t, "Exchange"), RoutingKey: StaticString(t, "RoutingKey"))).ToList();

        routes.ShouldAllBe(r => ExchangeOwnerByNamespace[r.Type.Namespace!] == r.Exchange);
        routes.Select(r => r.RoutingKey).ShouldBeUnique();
        routes.Select(r => IntegrationEventNames.EventTypeOf(r.Type)).ShouldBeUnique();
    }

    [Fact]
    public void Wire_schema_matches_the_approved_snapshot()
    {
        var received = BuildSchema();
        var approvedPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "contracts.approved.txt");
        var approved = File.Exists(approvedPath) ? File.ReadAllText(approvedPath).ReplaceLineEndings("\n") : "";

        if (received != approved)
        {
            File.WriteAllText(Path.Combine(SourceDirectory(), "Snapshots", "contracts.received.txt"), received);
            Assert.Fail("Integration event schema changed. If intentional (and backwards compatible), "
                        + "copy Snapshots/contracts.received.txt over contracts.approved.txt.");
        }
    }

    private static string BuildSchema()
    {
        var naming = JsonNamingPolicy.CamelCase;
        var schema = new StringBuilder();
        foreach (var type in Events)
        {
            schema.Append(StaticString(type, "RoutingKey")).Append(" (").Append(IntegrationEventNames.EventTypeOf(type)).Append(")\n");
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                schema.Append("  ").Append(naming.ConvertName(property.Name)).Append(": ").Append(Describe(property.PropertyType)).Append('\n');
            }
        }

        return schema.ToString();
    }

    private static string Describe(Type type) =>
        Nullable.GetUnderlyingType(type) is { } inner ? Describe(inner) + "?"
        : type == typeof(AddressDto) ? "{ street: String, city: String, postalCode: String }"
        : type.Name;

    private static string StaticString(Type type, string name) =>
        (string)type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    private static string SourceDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
}
