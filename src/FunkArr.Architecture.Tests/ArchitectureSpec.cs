using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Assembly = System.Reflection.Assembly;

namespace FunkArr.Architecture.Tests;

public sealed class ArchitectureSpec
{
    private static readonly Assembly _messagesAssembly = typeof(Messages.AssemblyMarker).Assembly;
    private static readonly Assembly _persistenceAssembly = typeof(Persistence.AssemblyMarker).Assembly;
    private static readonly Assembly _coreAssembly = typeof(Core.AssemblyMarker).Assembly;
    private static readonly Assembly _searchAssembly = typeof(Search.AssemblyMarker).Assembly;
    private static readonly Assembly _downloadAssembly = typeof(Download.AssemblyMarker).Assembly;
    private static readonly Assembly _ruleSetAssembly = typeof(RuleSet.AssemblyMarker).Assembly;
    private static readonly Assembly _matchMagicAssembly = typeof(MatchMagic.AssemblyMarker).Assembly;
    private static readonly Assembly _metadataResolverAssembly = typeof(MetadataResolver.AssemblyMarker).Assembly;
    private static readonly Assembly _apiAssembly = typeof(Api.AssemblyMarker).Assembly;
    private static readonly Assembly _arrApiAssembly = typeof(ArrApi.AssemblyMarker).Assembly;

    private static readonly ArchUnitNET.Domain.Architecture _architecture =
        new ArchLoader()
            .LoadAssemblies(
                _messagesAssembly,
                _persistenceAssembly,
                _coreAssembly,
                _searchAssembly,
                _downloadAssembly,
                _ruleSetAssembly,
                _matchMagicAssembly,
                _metadataResolverAssembly,
                _apiAssembly,
                _arrApiAssembly)
            .Build();

    private static IObjectProvider<IType> InAssembly(Assembly assembly)
        => Types().That().ResideInAssembly(assembly);

    private static readonly IObjectProvider<IType> _messagesLayer = InAssembly(_messagesAssembly);
    private static readonly IObjectProvider<IType> _persistenceLayer = InAssembly(_persistenceAssembly);
    private static readonly IObjectProvider<IType> _coreLayer = InAssembly(_coreAssembly);
    private static readonly IObjectProvider<IType> _searchLayer = InAssembly(_searchAssembly);
    private static readonly IObjectProvider<IType> _downloadLayer = InAssembly(_downloadAssembly);
    private static readonly IObjectProvider<IType> _ruleSetLayer = InAssembly(_ruleSetAssembly);
    private static readonly IObjectProvider<IType> _matchMagicLayer = InAssembly(_matchMagicAssembly);
    private static readonly IObjectProvider<IType> _metadataResolverLayer = InAssembly(_metadataResolverAssembly);
    private static readonly IObjectProvider<IType> _apiLayer = InAssembly(_apiAssembly);
    private static readonly IObjectProvider<IType> _arrApiLayer = InAssembly(_arrApiAssembly);

    [Fact]
    public void Messages_should_not_depend_on_any_project()
    {
        Types().That().Are(_messagesLayer)
            .Should().NotDependOnAnyTypesThat().Are(_persistenceLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_coreLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);
    }

    [Fact]
    public void Persistence_should_not_depend_on_layers_above_messages()
    {
        Types().That().Are(_persistenceLayer)
            .Should().NotDependOnAnyTypesThat().Are(_coreLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);
    }

    [Fact]
    public void Search_should_not_depend_on_other_domains()
    {
        Types().That().Are(_searchLayer)
            .Should().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .Check(_architecture);
    }

    [Fact]
    public void Download_should_not_depend_on_other_domains()
    {
        Types().That().Are(_downloadLayer)
            .Should().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .Check(_architecture);
    }

    [Fact]
    public void RuleSet_should_not_depend_on_other_domains()
    {
        Types().That().Are(_ruleSetLayer)
            .Should().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .Check(_architecture);
    }

    [Fact]
    public void MatchMagic_should_not_depend_on_other_domains()
    {
        Types().That().Are(_matchMagicLayer)
            .Should().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_metadataResolverLayer)
            .Check(_architecture);
    }

    [Fact]
    public void MetadataResolver_should_not_depend_on_other_domains()
    {
        Types().That().Are(_metadataResolverLayer)
            .Should().NotDependOnAnyTypesThat().Are(_searchLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_downloadLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_ruleSetLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_matchMagicLayer)
            .Check(_architecture);
    }

    [Fact]
    public void Domains_should_not_depend_on_adapters()
    {
        Types().That().Are(_searchLayer)
            .Should().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);

        Types().That().Are(_downloadLayer)
            .Should().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);

        Types().That().Are(_ruleSetLayer)
            .Should().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);

        Types().That().Are(_matchMagicLayer)
            .Should().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);

        Types().That().Are(_metadataResolverLayer)
            .Should().NotDependOnAnyTypesThat().Are(_apiLayer)
            .AndShould().NotDependOnAnyTypesThat().Are(_arrApiLayer)
            .Check(_architecture);
    }

    [Fact]
    public void Messages_should_not_depend_on_akka()
    {
        Types().That().Are(_messagesLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Akka")
            .Check(_architecture);
    }

    [Fact]
    public void Persistence_should_not_depend_on_akka()
    {
        Types().That().Are(_persistenceLayer)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace("Akka")
            .Check(_architecture);
    }

    [Fact]
    public void All_types_should_be_sealed()
    {
        var allLayers = new[]
        {
            _messagesLayer, _persistenceLayer, _searchLayer, _downloadLayer,
            _ruleSetLayer, _matchMagicLayer, _metadataResolverLayer
        };

        foreach (var layer in allLayers)
        {
            Classes().That().Are(layer)
                .And().AreNotAbstract()
                .And().DoNotHaveName("AssemblyMarker")
                .Should().BeSealed()
                .Check(_architecture);
        }
    }
}
