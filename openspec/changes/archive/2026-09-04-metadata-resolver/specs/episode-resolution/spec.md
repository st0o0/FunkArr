## MODIFIED Requirements

### Requirement: Episode resolution applies strategies in priority order
The episode resolver SHALL attempt to resolve each EpisodeCandidate to a TVDB episode using strategies in the following priority order: RegexExtracted, FuzzyTitleMatch, AirdateMatch, RuntimeWindow. The first strategy that produces a confident match SHALL win. If no strategy produces a match, the item SHALL remain unresolved. The class SHALL reside in the `FunkArr.MetadataResolver` namespace (renamed from `FunkArr.EpisodeGuide`).

#### Scenario: Regex-extracted season/episode passes through
- **WHEN** an EpisodeCandidate has ExistingSeason="2026" and ExistingEpisode="01"
- **THEN** the resolver SHALL return a ResolvedEpisode with Season="2026", Episode="01", Strategy="RegexExtracted", Confidence=1.0 without querying TVDB

#### Scenario: Fuzzy title match resolves an episode
- **WHEN** an EpisodeCandidate has Title="Roomservice" and TVDB episode "Roomservice" exists for the series
- **THEN** the resolver SHALL return a ResolvedEpisode with the TVDB season/episode numbers, Strategy="FuzzyTitleMatch", and Confidence equal to the similarity score

#### Scenario: Airdate match resolves an episode
- **WHEN** an EpisodeCandidate has AiredAt=2026-03-01 and a TVDB episode aired on 2026-03-01
- **THEN** the resolver SHALL return a ResolvedEpisode with that episode's season/episode numbers, Strategy="AirdateMatch", Confidence=0.9

#### Scenario: No strategy matches
- **WHEN** no strategy produces a confident match for an EpisodeCandidate
- **THEN** the resolver SHALL not include that item in the resolved results

#### Scenario: Strategy priority order
- **WHEN** both FuzzyTitleMatch and AirdateMatch would produce a result for the same candidate
- **THEN** FuzzyTitleMatch SHALL take priority (it runs first)

### Requirement: LevenshteinDistance namespace
The LevenshteinDistance utility SHALL reside in the `FunkArr.MetadataResolver` namespace (renamed from `FunkArr.EpisodeGuide`).

#### Scenario: Namespace
- **WHEN** LevenshteinDistance is referenced
- **THEN** it SHALL be in the `FunkArr.MetadataResolver` namespace
