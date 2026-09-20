# scoring-engine

## Purpose

Defines the ScoringEngine static class in FunkArr.Scoring that contains all scoring logic extracted from ScoringActor: filter evaluation, identification strategies, title construction, regex capture, German date extraction, umlaut normalization, and metadata building. Pure computation with no Akka.NET dependencies.

## Requirements

### Requirement: ScoringEngine is a pure static class

The `ScoringEngine` SHALL be a `static class` in `FunkArr.Scoring` with no Akka.NET dependencies. It SHALL contain all scoring logic currently in `ScoringActor`: filter evaluation, identification strategies, title construction, regex capture, German date extraction, umlaut normalization, and metadata building.

#### Scenario: No Akka dependency

- **WHEN** `ScoringEngine` is compiled
- **THEN** it SHALL have no references to any `Akka.*` namespace

#### Scenario: Class is static

- **WHEN** `ScoringEngine` is defined
- **THEN** it SHALL be a `static class` — not instantiable, no instance state

### Requirement: ScoringEngine exposes a single public Score method

The `ScoringEngine` SHALL expose one public static method `Score` that accepts the scoring configuration (rule set ID, default confidence, rules) and an array of candidates, and returns scored items with traces.

#### Scenario: Score with matching rules

- **WHEN** `Score` is called with a config containing rules and an array of candidates
- **THEN** it SHALL return a `ScoredItem[]` and `ItemTrace[]` of the same length as the input candidates

#### Scenario: Score with empty candidates

- **WHEN** `Score` is called with an empty candidates array
- **THEN** it SHALL return empty arrays for both scored items and traces

#### Scenario: Score applies rules in priority order

- **WHEN** `Score` is called with rules at priority 0 and 10
- **THEN** priority 0 SHALL be evaluated first, and the first matching rule wins

### Requirement: ScoringEngine filter evaluation produces traces

The `ScoringEngine` SHALL evaluate filter groups (All, Any, Not) with short-circuit semantics and produce `FilterGroupTrace` records. All filter evaluation logic from `ScoringActor` SHALL be preserved identically.

#### Scenario: All group short-circuits on failure

- **WHEN** an All group's second condition fails
- **THEN** the third condition SHALL be marked Skipped in the trace

#### Scenario: Any group short-circuits on success

- **WHEN** an Any group's first condition passes
- **THEN** remaining conditions SHALL be marked Skipped in the trace

### Requirement: ScoringEngine identification strategies produce traces

The `ScoringEngine` SHALL support all five identification strategies: SeasonAndEpisodeNumber, AbsoluteEpisodeNumber, TitleExact, TitleIncludes, AirdateExtraction. Each SHALL produce an `IdentificationTrace` with the strategy name, attempted flag, and optional detail.

#### Scenario: All strategies preserved

- **WHEN** the ScoringEngine identification method is called
- **THEN** it SHALL handle all five `IdentificationStrategy` enum values identically to the current `ScoringActor` implementation

### Requirement: MetadataSpec includes ConstructedTitle
`MetadataSpec` SHALL include an optional `string? ConstructedTitle` field alongside Season, Episode, and AiredAt. The `ScoringEngine.BuildMetadata` method SHALL preserve the `TracedIdentification.Title` value as `ConstructedTitle` instead of dropping it.

#### Scenario: BuildMetadata preserves constructed title from TitleParts
- **WHEN** a scoring rule with TitleParts strategy matches and `TracedIdentification.Title` is "Roomservice"
- **THEN** the resulting `MetadataSpec.ConstructedTitle` SHALL be "Roomservice"

#### Scenario: BuildMetadata with regex-extracted identification
- **WHEN** a scoring rule with SeasonAndEpisodeNumber strategy matches and `TracedIdentification.Title` is null
- **THEN** the resulting `MetadataSpec.ConstructedTitle` SHALL be null

#### Scenario: Existing MetadataSpec callers unaffected
- **WHEN** code constructs `MetadataSpec` without specifying ConstructedTitle
- **THEN** ConstructedTitle SHALL default to null (backwards compatible)

### Requirement: ScoringEngine uses 100ms regex timeout

All regex evaluations in `ScoringEngine` SHALL use a 100ms timeout to prevent catastrophic backtracking.

#### Scenario: Regex timeout on pathological input

- **WHEN** a regex pattern causes backtracking exceeding 100ms
- **THEN** the evaluation SHALL return false (for filters) or null (for captures) without throwing
