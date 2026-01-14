using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FamilyFitness.UnitTests.ArchitectureTests;

public class LayeringArchitectureTests
{
    private static readonly ArchUnitNET.Domain.Architecture FamilyFitnessArchitecture = new ArchLoader()
        .LoadAssemblies(
            System.Reflection.Assembly.Load("FamilyFitness.Domain"),
            System.Reflection.Assembly.Load("FamilyFitness.Application"),
            System.Reflection.Assembly.Load("FamilyFitness.Infrastructure"),
            System.Reflection.Assembly.Load("FamilyFitness.Api"),
            System.Reflection.Assembly.Load("FamilyFitness.Blazor")
        )
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly("FamilyFitness.Domain").As("Domain");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly("FamilyFitness.Application").As("Application");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly("FamilyFitness.Infrastructure").As("Infrastructure");

    private static readonly IObjectProvider<IType> ApiLayer =
        Types().That().ResideInAssembly("FamilyFitness.Api").As("API");

    private static readonly IObjectProvider<IType> BlazorLayer =
        Types().That().ResideInAssembly("FamilyFitness.Blazor").As("Blazor");

    private static readonly IObjectProvider<IType> EfCoreFramework =
        Types().That().ResideInNamespace("Microsoft.EntityFrameworkCore").Or().ResideInNamespace("Microsoft.EntityFrameworkCore.*")
            .As("EF Core");

    private static readonly IObjectProvider<IType> AspNetFramework =
        Types().That().ResideInNamespace("Microsoft.AspNetCore").Or().ResideInNamespace("Microsoft.AspNetCore.*")
            .As("ASP.NET");

    private static readonly IObjectProvider<IType> MicrosoftExtensionsFramework =
        Types().That().ResideInNamespace("Microsoft.Extensions").Or().ResideInNamespace("Microsoft.Extensions.*")
            .As("Microsoft.Extensions");

    private static readonly IObjectProvider<IType> NonDomainLayers =
        Types().That().Are(ApplicationLayer).Or().Are(InfrastructureLayer).Or().Are(ApiLayer).Or().Are(BlazorLayer)
            .As("Non-domain layers");

    private static readonly IObjectProvider<IType> NonApplicationLayers =
        Types().That().Are(InfrastructureLayer).Or().Are(ApiLayer).Or().Are(BlazorLayer)
            .As("Non-application layers");

    private static readonly IObjectProvider<IType> PresentationLayers =
        Types().That().Are(ApiLayer).Or().Are(BlazorLayer)
            .As("Presentation layers");

    private static readonly IObjectProvider<IType> FrameworkDependencies =
        Types().That().Are(EfCoreFramework).Or().Are(AspNetFramework).Or().Are(MicrosoftExtensionsFramework)
            .As("Framework dependencies");

    private static readonly IObjectProvider<IType> AspNetOrEfCoreDependencies =
        Types().That().Are(EfCoreFramework).Or().Are(AspNetFramework)
            .As("ASP.NET or EF Core dependencies");

    [Fact]
    public void Domain_ShouldNotDependOn_OtherLayers()
    {
        IArchRule rule = Types().That().Are(DomainLayer).Should().NotDependOnAny(NonDomainLayers)
            .Because("Domain must be layer-independent").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureApiOrBlazor()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer).Should().NotDependOnAny(NonApplicationLayers)
            .Because("Application must not depend on outer layers").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_ApiOrBlazor()
    {
        IArchRule rule = Types().That().Are(InfrastructureLayer).Should().NotDependOnAny(PresentationLayers)
            .Because("Infrastructure must not depend on presentation layers").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Api_ShouldNotDependOn_Blazor()
    {
        IArchRule rule = Types().That().Are(ApiLayer).Should().NotDependOnAny(BlazorLayer)
            .Because("API must not depend on UI").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Blazor_ShouldNotDependOn_Infrastructure()
    {
        IArchRule rule = Types().That().Are(BlazorLayer).Should().NotDependOnAny(InfrastructureLayer)
            .Because("UI must not depend on Infrastructure").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Domain_ShouldNotDependOn_AspNetEfOrExtensions()
    {
        IArchRule rule = Types().That().Are(DomainLayer).Should().NotDependOnAny(FrameworkDependencies)
            .Because("Domain must stay free of infrastructure/framework dependencies").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }

    [Fact]
    public void Application_ShouldNotDependOn_AspNetOrEfCore()
    {
        IArchRule rule = Types().That().Are(ApplicationLayer).Should().NotDependOnAny(AspNetOrEfCoreDependencies)
            .Because("Application must stay free of infrastructure/framework dependencies").WithoutRequiringPositiveResults();

        rule.Check(FamilyFitnessArchitecture);
    }
}
