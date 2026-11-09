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
    [InlineData(PersistedMediaType.Unknown, 0)]
    [InlineData(PersistedMediaType.Show, 1)]
    [InlineData(PersistedMediaType.Movie, 2)]
    public void PersistedMediaType_HasFrozenValues(PersistedMediaType member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedFailureKind.Unknown, 0)]
    [InlineData(PersistedFailureKind.Transient, 1)]
    [InlineData(PersistedFailureKind.Permanent, 2)]
    public void PersistedFailureKind_HasFrozenValues(PersistedFailureKind member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedMatchMethod.Unknown, 0)]
    [InlineData(PersistedMatchMethod.RegexExtracted, 1)]
    [InlineData(PersistedMatchMethod.TitleMatch, 2)]
    [InlineData(PersistedMatchMethod.AirdateMatch, 3)]
    [InlineData(PersistedMatchMethod.YearMatch, 4)]
    public void PersistedMatchMethod_HasFrozenValues(PersistedMatchMethod member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedMediathekVariant.Unknown, 0)]
    [InlineData(PersistedMediathekVariant.Standard, 1)]
    [InlineData(PersistedMediathekVariant.AudioDescription, 2)]
    [InlineData(PersistedMediathekVariant.SignLanguage, 3)]
    [InlineData(PersistedMediathekVariant.PlainLanguage, 4)]
    [InlineData(PersistedMediathekVariant.OriginalVersion, 5)]
    public void PersistedMediathekVariant_HasFrozenValues(PersistedMediathekVariant member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedRuleOutcome.Unknown, 0)]
    [InlineData(PersistedRuleOutcome.Matched, 1)]
    [InlineData(PersistedRuleOutcome.FilterFailed, 2)]
    [InlineData(PersistedRuleOutcome.IdentificationFailed, 3)]
    public void PersistedRuleOutcome_HasFrozenValues(PersistedRuleOutcome member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedFilterGroupOp.Unknown, 0)]
    [InlineData(PersistedFilterGroupOp.All, 1)]
    [InlineData(PersistedFilterGroupOp.Any, 2)]
    [InlineData(PersistedFilterGroupOp.Not, 3)]
    public void PersistedFilterGroupOp_HasFrozenValues(PersistedFilterGroupOp member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedIdentificationStrategy.Unknown, 0)]
    [InlineData(PersistedIdentificationStrategy.SeasonAndEpisodeNumber, 1)]
    [InlineData(PersistedIdentificationStrategy.AbsoluteEpisodeNumber, 2)]
    [InlineData(PersistedIdentificationStrategy.TitleExact, 3)]
    [InlineData(PersistedIdentificationStrategy.TitleIncludes, 4)]
    [InlineData(PersistedIdentificationStrategy.AirdateExtraction, 5)]
    public void PersistedIdentificationStrategy_HasFrozenValues(PersistedIdentificationStrategy member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedIdentificationFailureReason.Unknown, 0)]
    [InlineData(PersistedIdentificationFailureReason.UnknownStrategy, 1)]
    [InlineData(PersistedIdentificationFailureReason.SeasonPatternNotMatched, 2)]
    [InlineData(PersistedIdentificationFailureReason.NoEpisodePatternConfigured, 3)]
    [InlineData(PersistedIdentificationFailureReason.EpisodePatternNotMatched, 4)]
    [InlineData(PersistedIdentificationFailureReason.NoTitlePartsConfigured, 5)]
    [InlineData(PersistedIdentificationFailureReason.TitlePartRegexNotMatched, 6)]
    [InlineData(PersistedIdentificationFailureReason.TitleDoesNotMatch, 7)]
    [InlineData(PersistedIdentificationFailureReason.NoDateFoundInTitle, 8)]
    public void PersistedIdentificationFailureReason_HasFrozenValues(PersistedIdentificationFailureReason member, int expected) =>
        Assert.Equal(expected, (int)member);

    [Theory]
    [InlineData(PersistedSearchSource.Unknown, 0)]
    [InlineData(PersistedSearchSource.Sonarr, 1)]
    [InlineData(PersistedSearchSource.Radarr, 2)]
    [InlineData(PersistedSearchSource.Prowlarr, 3)]
    [InlineData(PersistedSearchSource.Feed, 5)]
    public void PersistedSearchSource_HasFrozenValues(PersistedSearchSource member, int expected) =>
        Assert.Equal(expected, (int)member);

    private static void AssertRoundtrip<TDomain, TPersisted>(Func<TDomain, TPersisted> toPersistence, Func<TPersisted, TDomain> toDomain)
        where TDomain : struct, Enum
        where TPersisted : struct, Enum
    {
        var domainValues = Enum.GetValues<TDomain>().Where(v => v.ToString() != "Unknown").ToArray();
        var persistedValues = Enum.GetValues<TPersisted>().Where(v => v.ToString() != "Unknown").ToArray();
        Assert.Equal(domainValues.Length, persistedValues.Length);

        foreach (var domain in domainValues)
        {
            var persisted = toPersistence(domain);
            Assert.Equal(domain.ToString(), persisted.ToString());
            Assert.Equal(domain, toDomain(persisted));
        }
    }
}
