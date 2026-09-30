## MODIFIED Requirements

### Requirement: Match trace emission
The matching engine SHALL produce an `ItemEvaluation` for every Mediathek result item evaluated, recording the outcome via `EvaluationOutcome` enum and the complete evaluation path through all rules attempted. Matched items SHALL carry the full `MatchedEpisodeInfo` through the response to callers. Each `ItemEvaluation` SHALL include `RuleEvaluation[]` with `FilterCheck[]` and `StrategyDetail` for every rule tested (not only the winning rule for matched items).

#### Scenario: Matched item produces evaluation with full pipeline
- **WHEN** item "Sturm der Liebe (1622)" matches via rule #1 after rule #0 fails
- **THEN** the ItemEvaluation SHALL have Outcome=Matched (0), and RuleEvaluations SHALL contain two entries: rule #0 with Outcome=NoMatch and rule #1 with Outcome=Matched, each with FilterChecks and StrategyDetail

#### Scenario: MatchedEpisodeInfo preserved in MatchedItemInfo
- **WHEN** item "Tatort - Der letzte Schrei" matches to TVDB episode S01E03 "Der letzte Schrei" aired 2024-03-15
- **THEN** the `MatchedItemInfo` response SHALL carry the full `MatchedEpisodeInfo` with Episode.AiredSeason=1, Episode.AiredEpisodeNumber=3, Episode.EpisodeName="Der letzte Schrei", Episode.FirstAired="2024-03-15"

#### Scenario: Filtered item produces evaluation without rule pipeline
- **WHEN** item "Tatort (AD)" is excluded by ContentFilter.ShouldSkip
- **THEN** the ItemEvaluation SHALL have Outcome=Filtered (1), FilterReason="accessibility-skip", and RuleEvaluations SHALL be empty

#### Scenario: Unmatched item produces evaluation with all rule failures
- **WHEN** item "Sturm der Liebe Highlights" fails all 2 rules
- **THEN** the ItemEvaluation SHALL have Outcome=Unmatched (2) and RuleEvaluations SHALL contain 2 entries with FilterChecks and StrategyDetail for each

#### Scenario: Filter checks capture all individual filters
- **WHEN** a rule has filters `all: [duration > 35, channel eq "ARD"]` and the item has duration 2940s (49min) from "Das Erste"
- **THEN** the FilterChecks SHALL contain two entries: (field="duration", op=GreaterThan, value="35", actual="49", passed=true) and (field="channel", op=Eq, value="ARD", actual="Das Erste", passed=false)

#### Scenario: Strategy detail captures regex for season/episode strategy
- **WHEN** the seasonAndEpisodeNumber strategy applies seasonRegex `S(\d+)` and episodeRegex `E(\d+)` to title "Folge 8 (S11/E08)"
- **THEN** the StrategyDetail SHALL have RegexPattern containing both patterns, RegexInput="Folge 8 (S11/E08)", and CapturedValue containing extracted values

#### Scenario: Strategy detail captures constructed title
- **WHEN** the itemTitleExact strategy builds title "Die goldene Zeit" from title rules
- **THEN** the StrategyDetail SHALL have ConstructedTitle="Die goldene Zeit"

#### Scenario: Strategy detail captures TVDB match
- **WHEN** the strategy finds TVDB episode "Folge 1622" for absolute episode 1622
- **THEN** the StrategyDetail SHALL have TvdbMatch="Folge 1622"

### Requirement: EvaluateRulesWithTraces return type
The `EvaluateRulesWithTraces` method SHALL return `(IReadOnlyList<MatchedEpisodeInfo> Matches, IReadOnlyList<ItemEvaluation> Items)` where Items contains one `ItemEvaluation` per input item with full pipeline detail.

#### Scenario: Return shape
- **WHEN** `EvaluateRulesWithTraces` is called with 200 items and 2 rules
- **THEN** the Items list SHALL contain exactly 200 ItemEvaluation records, and the Matches list SHALL contain the subset that matched

### Requirement: EvaluateMovieRulesWithTraces return type
The `EvaluateMovieRulesWithTraces` method SHALL return `(IReadOnlyList<MatchedItemInfo> Matches, IReadOnlyList<ItemEvaluation> Items)` with the same unified ItemEvaluation structure as the TV variant.

#### Scenario: Movie evaluation return shape
- **WHEN** `EvaluateMovieRulesWithTraces` is called with 50 items
- **THEN** the Items list SHALL contain exactly 50 ItemEvaluation records with RuleEvaluation pipeline detail
