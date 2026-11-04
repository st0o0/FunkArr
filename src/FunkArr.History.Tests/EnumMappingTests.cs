using FunkArr.Messages;
using FunkArr.Messages.Enrichment;
using FunkArr.Messages.Scoring;
using FunkArr.Messages.Scoring.History;
using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.History.Tests;

public sealed class EnumMappingTests
{
    [Fact]
    public void MediathekVariant_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MediathekVariant, PersistedMediathekVariant>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void RuleOutcome_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<RuleOutcome, PersistedRuleOutcome>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void FilterGroupOp_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<FilterGroupOp, PersistedFilterGroupOp>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void IdentificationStrategy_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<IdentificationStrategy, PersistedIdentificationStrategy>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void IdentificationFailureReason_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<IdentificationFailureReason, PersistedIdentificationFailureReason>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void MatchMethod_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MatchMethod, PersistedMatchMethod>(v => v.ToPersistence(), v => v.ToDomain());

    [Fact]
    public void MatchMethod_UndefinedDomainValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((MatchMethod)99).ToPersistence());

        Assert.Equal("value", ex.ParamName);
        Assert.Equal((MatchMethod)99, ex.ActualValue);
    }

    [Fact]
    public void RuleOutcome_UndefinedPersistedValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((PersistedRuleOutcome)99).ToDomain());

        Assert.Equal("value", ex.ParamName);
        Assert.Equal((PersistedRuleOutcome)99, ex.ActualValue);
    }

    [Theory]
    [InlineData(PersistedMediaType.Show, 0)]
    [InlineData(PersistedMediaType.Movie, 1)]
    public void PersistedMediaType_HasFrozenValues(PersistedMediaType member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedFailureKind.Transient, 0)]
    [InlineData(PersistedFailureKind.Permanent, 1)]
    public void PersistedFailureKind_HasFrozenValues(PersistedFailureKind member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedMatchMethod.RegexExtracted, 0)]
    [InlineData(PersistedMatchMethod.TitleMatch, 1)]
    [InlineData(PersistedMatchMethod.AirdateMatch, 2)]
    [InlineData(PersistedMatchMethod.YearMatch, 3)]
    public void PersistedMatchMethod_HasFrozenValues(PersistedMatchMethod member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedMediathekVariant.Standard, 0)]
    [InlineData(PersistedMediathekVariant.AudioDescription, 1)]
    [InlineData(PersistedMediathekVariant.SignLanguage, 2)]
    [InlineData(PersistedMediathekVariant.PlainLanguage, 3)]
    [InlineData(PersistedMediathekVariant.OriginalVersion, 4)]
    public void PersistedMediathekVariant_HasFrozenValues(PersistedMediathekVariant member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedRuleOutcome.Matched, 0)]
    [InlineData(PersistedRuleOutcome.FilterFailed, 1)]
    [InlineData(PersistedRuleOutcome.IdentificationFailed, 2)]
    public void PersistedRuleOutcome_HasFrozenValues(PersistedRuleOutcome member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedFilterGroupOp.All, 0)]
    [InlineData(PersistedFilterGroupOp.Any, 1)]
    [InlineData(PersistedFilterGroupOp.Not, 2)]
    public void PersistedFilterGroupOp_HasFrozenValues(PersistedFilterGroupOp member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedIdentificationStrategy.SeasonAndEpisodeNumber, 0)]
    [InlineData(PersistedIdentificationStrategy.AbsoluteEpisodeNumber, 1)]
    [InlineData(PersistedIdentificationStrategy.TitleExact, 2)]
    [InlineData(PersistedIdentificationStrategy.TitleIncludes, 3)]
    [InlineData(PersistedIdentificationStrategy.AirdateExtraction, 4)]
    public void PersistedIdentificationStrategy_HasFrozenValues(PersistedIdentificationStrategy member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedIdentificationFailureReason.UnknownStrategy, 0)]
    [InlineData(PersistedIdentificationFailureReason.SeasonPatternNotMatched, 1)]
    [InlineData(PersistedIdentificationFailureReason.NoEpisodePatternConfigured, 2)]
    [InlineData(PersistedIdentificationFailureReason.EpisodePatternNotMatched, 3)]
    [InlineData(PersistedIdentificationFailureReason.NoTitlePartsConfigured, 4)]
    [InlineData(PersistedIdentificationFailureReason.TitlePartRegexNotMatched, 5)]
    [InlineData(PersistedIdentificationFailureReason.TitleDoesNotMatch, 6)]
    [InlineData(PersistedIdentificationFailureReason.NoDateFoundInTitle, 7)]
    public void PersistedIdentificationFailureReason_HasFrozenValues(PersistedIdentificationFailureReason member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedSearchSource.Sonarr, 0)]
    [InlineData(PersistedSearchSource.Radarr, 1)]
    [InlineData(PersistedSearchSource.Prowlarr, 2)]
    [InlineData(PersistedSearchSource.Feed, 4)]
    public void PersistedSearchSource_HasFrozenValues(PersistedSearchSource member, int expected) =>
        Assert.Equal(expected, (int)member);

    private static void AssertRoundtrip<TDomain, TPersisted>(Func<TDomain, TPersisted> toPersistence, Func<TPersisted, TDomain> toDomain)
        where TDomain : struct, Enum
        where TPersisted : struct, Enum
    {
        var domainValues = Enum.GetValues<TDomain>().Where(v => v.ToString() != "Unknown").ToArray();
        var persistedValues = Enum.GetValues<TPersisted>();
        Assert.Equal(domainValues.Length, persistedValues.Length);

        foreach (var domain in domainValues)
        {
            var persisted = toPersistence(domain);
            Assert.Equal(domain.ToString(), persisted.ToString());
            Assert.Equal(domain, toDomain(persisted));
        }
    }
}
