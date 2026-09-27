using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;
using FunkArr.Persistence.Events.Shared;

namespace FunkArr.Tests.Shared;

public static class TestItemTraceBuilder
{
    public static PersistedItemTrace CreateSampleTrace() =>
        new(
            Candidate: new PersistedScoreCandidate(
                "Tatort: Der letzte Schrei", "Tatort", "ARD",
                5400, 720, "Ein spannender Fall", 1700000000),
            Matched: true,
            Score: 0.95,
            MatchedRuleId: "rule-1",
            Identification: new PersistedTracedIdentification("1", "5", "Der letzte Schrei"),
            RuleTraces:
            [
                new PersistedRuleTrace(
                    "rule-1", 1, PersistedRuleOutcome.Matched,
                    new PersistedFilterGroupTrace(PersistedFilterGroupOp.All, true,
                    [
                        new PersistedFilterNodeTrace("title", "contains", "Tatort",
                            "Tatort: Der letzte Schrei", true, false, null)
                    ]),
                    new PersistedIdentificationTrace(PersistedIdentificationStrategy.SeasonAndEpisodeNumber, true, null))
            ],
            EnrichmentTrace: new PersistedEnrichmentTrace(
                PersistedMatchMethod.TitleMatch, 0.9f, true,
                ResolvedSeason: "1", ResolvedEpisode: "5",
                ResolvedTitle: "Der letzte Schrei"));
}
