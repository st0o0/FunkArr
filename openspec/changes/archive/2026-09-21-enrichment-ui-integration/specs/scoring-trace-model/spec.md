## ADDED Requirements

### Requirement: EnrichmentTrace record
The scoring trace model SHALL include an EnrichmentTrace record with: method (MatchMethod), confidence (float), enriched (bool), resolvedSeason (string?), resolvedEpisode (string?), resolvedTitle (string?), resolvedYear (int?), daysDiff (int?), detail (string?).

#### Scenario: Episode enrichment via title match
- **WHEN** enrichment resolves a matched item to TVDB episode S02E05 "Der Fall" via TitleMatch at 0.85 similarity
- **THEN** EnrichmentTrace contains method=TitleMatch, confidence=0.85, resolvedSeason="2", resolvedEpisode="5", resolvedTitle="Der Fall", enriched=true, daysDiff=null, detail=null

#### Scenario: Episode enrichment via airdate match
- **WHEN** enrichment resolves a matched item via AirdateMatch with 2 days difference at 0.71 confidence
- **THEN** EnrichmentTrace contains method=AirdateMatch, confidence=0.71, daysDiff=2, enriched=true

#### Scenario: Episode enrichment via regex-extracted lookup
- **WHEN** scoring already extracted S01E03 via regex and enrichment looks up the episode name from TVDB
- **THEN** EnrichmentTrace contains method=RegexExtracted, confidence=1.0, resolvedSeason="1", resolvedEpisode="3", resolvedTitle="Pilotfolge", enriched=true

#### Scenario: Failed enrichment with detail
- **WHEN** enrichment is enabled but title similarity (0.65) is below threshold (0.7)
- **THEN** EnrichmentTrace contains enriched=false, detail="similarity 0.65 < threshold 0.7"

#### Scenario: Failed enrichment no episodes found
- **WHEN** enrichment is enabled but TVDB returns no episodes for the given season
- **THEN** EnrichmentTrace contains enriched=false, detail="no episodes found"

#### Scenario: Movie enrichment
- **WHEN** enrichment resolves a matched item to TMDB movie "Tatort: Freitod" (2019) via TitleMatch
- **THEN** EnrichmentTrace contains method=TitleMatch, enriched=true, resolvedTitle="Tatort: Freitod", resolvedYear=2019

## MODIFIED Requirements

### Requirement: ItemTrace includes enrichment
The ItemTrace record SHALL include an optional EnrichmentTrace field that is null when enrichment was not requested and populated when enrichment was attempted for a matched item.

#### Scenario: ItemTrace without enrichment
- **WHEN** a test is run without enrichment config
- **THEN** ItemTrace.EnrichmentTrace is null

#### Scenario: ItemTrace with successful enrichment
- **WHEN** a test is run with enrichment config and the item matched and enrichment resolved it
- **THEN** ItemTrace.EnrichmentTrace contains the enrichment result with enriched=true

#### Scenario: ItemTrace with failed enrichment
- **WHEN** a test is run with enrichment config and the item matched but enrichment could not resolve it
- **THEN** ItemTrace.EnrichmentTrace contains enriched=false with a detail string explaining why

#### Scenario: Unmatched item has no enrichment trace
- **WHEN** a test is run with enrichment config but the item did not match any scoring rule
- **THEN** ItemTrace.EnrichmentTrace is null (enrichment only applies to scored matches)
