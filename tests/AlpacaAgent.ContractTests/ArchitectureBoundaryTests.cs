using AlpacaAgent.Application.Ports;
using AlpacaAgent.Domain.Contracts;

namespace AlpacaAgent.ContractTests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferenceForbiddenInfrastructureAssemblies()
    {
        var forbiddenPrefixes = new[] { "Npgsql", "Microsoft.EntityFrameworkCore", "Alpaca.Markets", "OpenAI", "Microsoft.Agents", "Azure.AI" };
        var assemblies = new[] { typeof(WorkflowCycleV1).Assembly, typeof(IDurableWorkflowKernel).Assembly };
        foreach (var assembly in assemblies)
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
                forbiddenPrefixes.Any(prefix => reference.Name!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void PolicyAssessment_ModelContractCannotRepresentExecutableOrderFields()
    {
        var forbidden = new[] { "ContractId", "Strike", "Expiration", "Quantity", "Side", "OrderType", "LimitPrice", "TimeInForce", "BrokerOperation" };
        var properties = typeof(PolicyAssessmentV1Contract).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(properties, name => forbidden.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplicationPorts_ExposeApplicationOwnedTypesOnly()
    {
        var portTypes = typeof(IDurableWorkflowKernel).Assembly.GetTypes().Where(type => type.IsInterface && type.Namespace == typeof(IDurableWorkflowKernel).Namespace);
        foreach (var method in portTypes.SelectMany(type => type.GetMethods()))
        {
            var exposed = method.GetParameters().Select(parameter => Unwrap(parameter.ParameterType)).Append(Unwrap(method.ReturnType));
            Assert.DoesNotContain(exposed, type => type.Namespace?.StartsWith("Npgsql", StringComparison.Ordinal) == true || type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
        }
    }

    private static Type Unwrap(Type type)
    {
        while (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>) || type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            type = type.GetGenericArguments()[0];
        return type;
    }
}
