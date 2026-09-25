using System.Reflection;

namespace Deliver.ArchitectureTests;

/// <summary>
/// Executable architecture rules. They fail the build when someone takes a shortcut, e.g. the domain
/// using EF Core, or Billing referencing Shipping code instead of its published contracts.
/// </summary>
public class ArchitectureTests
{
    private static readonly string[] LayeredContexts = ["Shipping", "Fleet", "Dispatch", "Billing"];
    private static readonly string[] AllContexts = [.. LayeredContexts, "Notifications", "Analytics"];

    private static readonly string[] InfrastructureFrameworks =
        ["Microsoft.EntityFrameworkCore", "Npgsql", "RabbitMQ.Client", "Microsoft.AspNetCore"];

    public static TheoryData<string> Contexts => new(LayeredContexts);

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Domain_depends_only_on_the_shared_kernel(string context)
    {
        var references = ReferencedAssemblies($"Deliver.{context}.Domain");

        references.Where(r => r.StartsWith("Deliver.", StringComparison.Ordinal))
            .ShouldAllBe(r => r == "Deliver.SharedKernel");
        references.ShouldNotContain(r => InfrastructureFrameworks.Any(f => r.StartsWith(f, StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Application_does_not_depend_on_infrastructure(string context)
    {
        var references = ReferencedAssemblies($"Deliver.{context}.Application");

        references.ShouldNotContain($"Deliver.{context}.Infrastructure");
        references.ShouldNotContain("Deliver.Messaging"); // only the transport-agnostic abstractions
        references.ShouldNotContain(r => InfrastructureFrameworks.Any(f => r.StartsWith(f, StringComparison.Ordinal)));
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Infrastructure_is_not_referenced_by_inner_layers(string context)
    {
        ReferencedAssemblies($"Deliver.{context}.Domain").ShouldNotContain($"Deliver.{context}.Infrastructure");
        ReferencedAssemblies($"Deliver.{context}.Application").ShouldNotContain($"Deliver.{context}.Infrastructure");
    }

    [Fact]
    public void Services_never_reference_another_service_only_the_shared_contracts()
    {
        foreach (var context in AllContexts)
        {
            var otherContexts = AllContexts.Where(c => c != context).Select(c => $"Deliver.{c}").ToList();

            foreach (var assembly in AssembliesOf(context))
            {
                var references = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
                var leaks = references.Where(r => otherContexts.Any(o => r == o || r.StartsWith(o + ".", StringComparison.Ordinal))).ToList();

                leaks.ShouldBeEmpty($"{assembly.GetName().Name} must talk to other services through events, not code references.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Contexts))]
    public void Aggregates_do_not_expose_public_setters(string context)
    {
        var domain = Assembly.Load($"Deliver.{context}.Domain");

        var publicSetters = domain.GetTypes()
            .Where(t => t.IsClass && IsAggregateRoot(t))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
            .ToList();

        publicSetters.ShouldBeEmpty();
    }

    private static bool IsAggregateRoot(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("AggregateRoot", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static List<string> ReferencedAssemblies(string assemblyName) =>
        Assembly.Load(assemblyName).GetReferencedAssemblies().Select(a => a.Name!).ToList();

    private static IEnumerable<Assembly> AssembliesOf(string context) =>
        LayeredContexts.Contains(context)
            ? new[] { "Domain", "Application", "Infrastructure", "Api" }.Select(layer => Assembly.Load($"Deliver.{context}.{layer}"))
            : [Assembly.Load($"Deliver.{context}")];
}
