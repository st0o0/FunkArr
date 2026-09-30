## ADDED Requirements

### Requirement: Core project has no external dependencies
The `FunkArr.Core` project SHALL have no PackageReference to Akka, HttpClient, ASP.NET, Newtonsoft, or any non-BCL dependency. It SHALL target net10.0 and contain only pure logic.

#### Scenario: Core project compiles without external packages
- **WHEN** `FunkArr.Core.csproj` is built
- **THEN** it SHALL compile with zero PackageReferences and only BCL/runtime dependencies

### Requirement: Core contains pure model types
The `FunkArr.Core` project SHALL contain all model types used by matching, scoring, filtering, and generation logic, including `Rule`, `Filter`, `FilterGroup`, `RuleSetFile`, `MediaReference`, `MatchingStrategy`, `FilterOp`, `TitleRule`, `MediathekResultItem`, `SearchResult`, `QualityInfo`, `TvdbEpisodeInfo`, `TmdbMovieInfo`, and `MatchedItemInfo`.

#### Scenario: Model types are importable from Core
- **WHEN** a type from `FunkArr.Messages` or `FunkArr` references `Rule` or `MediathekResultItem`
- **THEN** it SHALL resolve the type from the `FunkArr.Core` project

### Requirement: Core contains content filtering logic
The `ContentFilter` static class SHALL reside in `FunkArr.Core` and provide the `ShouldSkip` and `GetSkipReason` methods.

#### Scenario: ContentFilter is callable from Core
- **WHEN** code in `FunkArr.Core` calls `ContentFilter.ShouldSkip(title, topic)`
- **THEN** it SHALL compile and return the correct result without any external dependency

### Requirement: Core contains scoring and title building
`ResultScorer` and `ReleaseTitleBuilder` SHALL reside in `FunkArr.Core` as pure static classes.

#### Scenario: ResultScorer available from Core
- **WHEN** code references `ResultScorer.Score` or `ReleaseTitleBuilder.BuildStandard`
- **THEN** it SHALL resolve from `FunkArr.Core`

### Requirement: Core contains ruleset generation logic
`RuleSetGenerator` SHALL reside in `FunkArr.Core` as a pure static class with no I/O dependencies.

#### Scenario: RuleSetGenerator compiles in Core
- **WHEN** `RuleSetGenerator` is built as part of `FunkArr.Core`
- **THEN** it SHALL compile without Akka, HttpClient, or DI references
