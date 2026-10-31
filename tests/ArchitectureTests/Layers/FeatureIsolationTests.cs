using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Shouldly;
using Web.Api.Common;
using Web.Api.Features.Leases;

namespace ArchitectureTests.Layers;

public class FeatureIsolationTests : BaseTest
{
    private const string FeaturesNamespace = "Web.Api.Features.";

    private static readonly string[] Features = ["Properties", "Leases", "Portfolio", "Users"];

    private static readonly string[] SliceTypeNames =
        ["Handler", "Endpoint", "Validator", "Command", "Query", "Request", "Response"];

    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // ADR-001: un slice nunca referencia el Command/Query/Handler/Validator/Endpoint/Request/Response de otra feature.
    [Fact]
    public void Features_Should_NotReferenceSliceTypesOfOtherFeatures()
    {
        var violations = new List<string>();

        foreach (string feature in Features)
        {
            foreach (Type type in TypesOfFeature(feature))
            {
                foreach (Type dependency in Dependencies(type))
                {
                    string? owner = FeatureOf(dependency);
                    if (owner is not null && owner != feature && IsSliceType(dependency))
                    {
                        violations.Add($"{type.FullName} -> {dependency.FullName}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void IsolationCheck_Should_DetectCrossFeatureSliceReference()
    {
        // Sanity check: el detector sí encuentra una dependencia prohibida.
        Type[] dependencies = [.. Dependencies(typeof(FakeCoupledHandler))];

        dependencies.Any(d => FeatureOf(d) == "Users" && IsSliceType(d)).ShouldBeTrue();
    }

    [Fact]
    public void PureDomainFunctions_Should_NotDependOnFrameworks()
    {
        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .HaveName(nameof(LeaseStatement), nameof(TenantDocument), nameof(Period), nameof(LimaClock))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void EntityTypeConfigurations_Should_LiveInDatabaseConfigurations()
    {
        TestResult result = Types.InAssembly(WebApiAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .ResideInNamespace("Web.Api.Database.Configurations")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    private static IEnumerable<Type> TypesOfFeature(string feature) =>
        WebApiAssembly.GetTypes().Where(t => FeatureOf(t) == feature);

    private static string? FeatureOf(Type type)
    {
        string? ns = type.Namespace;
        if (ns is null || !ns.StartsWith(FeaturesNamespace, StringComparison.Ordinal))
        {
            return null;
        }

        string rest = ns[FeaturesNamespace.Length..];
        int dot = rest.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? rest : rest[..dot];
    }

    // Tipo anidado de una clase estática de slice (p. ej. CreateLease+Handler).
    private static bool IsSliceType(Type type) =>
        type.IsNested && SliceTypeNames.Contains(type.Name);

    private static HashSet<Type> Dependencies(Type type)
    {
        var found = new HashSet<Type>();

        void Add(Type? t)
        {
            if (t is null)
            {
                return;
            }

            if (t.HasElementType)
            {
                Add(t.GetElementType());
                return;
            }

            if (t.IsGenericType)
            {
                foreach (Type arg in t.GetGenericArguments())
                {
                    Add(arg);
                }

                t = t.GetGenericTypeDefinition();
            }

            found.Add(t);
        }

        Add(type.BaseType);
        foreach (Type i in type.GetInterfaces())
        {
            Add(i);
        }

        foreach (FieldInfo f in type.GetFields(AllMembers))
        {
            Add(f.FieldType);
        }

        foreach (PropertyInfo p in type.GetProperties(AllMembers))
        {
            Add(p.PropertyType);
        }

        foreach (MethodBase m in type.GetConstructors(AllMembers).Cast<MethodBase>().Concat(type.GetMethods(AllMembers)))
        {
            foreach (ParameterInfo p in m.GetParameters())
            {
                Add(p.ParameterType);
            }

            if (m is MethodInfo mi)
            {
                Add(mi.ReturnType);
            }
        }

        return found;
    }

    // Tipo de prueba que inyecta el handler de otra feature; valida que el detector funciona.
    private sealed class FakeCoupledHandler(Web.Api.Common.Messaging.ICommandHandler<Web.Api.Features.Users.Register.Command, Guid> other)
    {
        public Web.Api.Common.Messaging.ICommandHandler<Web.Api.Features.Users.Register.Command, Guid> Other { get; } = other;
    }
}
