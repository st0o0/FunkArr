## ADDED Requirements

### Requirement: Resolution strategy names are centralized constants

FunkArr.MetadataResolver SHALL define a static class `ResolutionStrategy` containing string constants for all resolution strategy names used in MovieResolver and EpisodeResolver.

#### Scenario: Strategy constants defined

- **WHEN** `ResolutionStrategy` is inspected
- **THEN** it SHALL contain constants for `"TitleMatch"`, `"YearMatch"`, `"FuzzyTitleMatch"`, `"AirdateMatch"`, and `"RegexExtracted"`
- **AND** all usages of these strings in resolvers SHALL reference these constants

### Requirement: "none" strategy check is a constant

The `"none"` string used to check for disabled resolution strategy in `TvdbResolverActor` and `EpisodeResolver` SHALL be a named constant.

#### Scenario: None strategy constant

- **WHEN** `TvdbResolverActor` or `EpisodeResolver` checks if a strategy is disabled
- **THEN** it SHALL compare against a named constant (e.g., `ResolutionStrategy.None`) instead of the literal string `"none"`
