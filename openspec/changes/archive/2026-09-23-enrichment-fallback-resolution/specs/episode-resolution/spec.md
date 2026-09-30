## MODIFIED Requirements

### Requirement: Episode resolution applies strategies in priority order
The episode resolver SHALL attempt to enrich each EpisodeCandidate to a TVDB episode using match methods. The resolver SHALL walk the `EnrichmentConfig.Methods` array in order, attempting each method until one produces a confident match. If a configured method produces a match, its TVDB-sourced Season/Episode SHALL be used. If no configured method matches and the candidate has ExistingSeason/ExistingEpisode (regex-extracted), those SHALL be used as fallback with Method=RegexExtracted and Confidence=1.0. If `EnrichmentConfig.Enabled` is false, the resolver SHALL return an empty array. Default method order is [Title, Airdate].

#### Scenario: Regex-extracted season/episode passes through
- **WHEN** an EpisodeCandidate has ExistingSeason="5" and ExistingEpisode="13"
- **AND** no configured enrichment method produces a confident TVDB match
- **THEN** the resolver SHALL return an EnrichedEpisode with Season="5", Episode="13", Method=MatchMethod.RegexExtracted, Confidence=1.0 as fallback

#### Scenario: Regex-extracted S/E overridden by TVDB match
- **WHEN** an EpisodeCandidate has ExistingSeason="31" and ExistingEpisode="03"
- **AND** TitleMatch resolves to TVDB Season="1", Episode="110" with confidence >= threshold
- **THEN** the resolver SHALL return Season="1", Episode="110", Method=MatchMethod.Title

#### Scenario: Methods array controls fallback order
- **WHEN** EnrichmentConfig.Methods is [Airdate, Title]
- **THEN** AirdateMatch SHALL be attempted before TitleMatch

#### Scenario: Default method order preserves current behavior
- **WHEN** EnrichmentConfig.Methods is [Title, Airdate] (the default)
- **THEN** TitleMatch SHALL be attempted before AirdateMatch, matching current hardcoded behavior

#### Scenario: Single method configured
- **WHEN** EnrichmentConfig.Methods is [Airdate]
- **THEN** only AirdateMatch SHALL be attempted; TitleMatch SHALL not run

#### Scenario: Enrichment disabled
- **WHEN** EnrichmentConfig.Enabled is false
- **THEN** the resolver SHALL return an empty array without attempting any matching

#### Scenario: No method matches
- **WHEN** no match method produces a confident match for an EpisodeCandidate
- **AND** the candidate has no ExistingSeason and no ExistingEpisode
- **THEN** the resolver SHALL not include that item in the enriched results
