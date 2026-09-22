using FunkArr.Persistence;
using FunkArr.Persistence.Events.ScoringHistory;

namespace FunkArr.Tests.Shared;

public static class TestItemTraceBuilder
{
    public static PersistedItemTrace CreateSampleTrace() =>
        new(
            CandidateTitle: "Tatort: Der letzte Schrei",
            CandidateTopic: "Tatort",
            CandidateChannel: "ARD",
            CandidateDuration: 5400,
            CandidateQuality: 720,
            CandidateDescription: "Ein spannender Fall",
            CandidateTimestamp: 1700000000,
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
