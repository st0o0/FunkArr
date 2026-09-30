## MODIFIED Requirements

### Requirement: SearchResultItem carries resolution metadata for all content types
The SearchResultItem record SHALL include optional ResolutionConfidence (float?) and ResolutionStrategy (string?) fields. These fields SHALL be populated for both TV show and movie search results when metadata resolution is performed.

#### Scenario: TV result with resolution metadata
- **WHEN** a TV search result is resolved via FuzzyTitleMatch with confidence 0.85
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=0.85 and ResolutionStrategy="FuzzyTitleMatch"

#### Scenario: Movie result with resolution metadata
- **WHEN** a movie search result is resolved via TmdbIdLookup with confidence 1.0
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=1.0 and ResolutionStrategy="TmdbIdLookup"

#### Scenario: Unresolved result
- **WHEN** a search result was not resolved (no TVDB/TMDB key or resolution failed)
- **THEN** the SearchResultItem SHALL have ResolutionConfidence=null and ResolutionStrategy=null

#### Scenario: Movie result with TMDB-validated year
- **WHEN** a movie result is enriched via TMDB resolution
- **THEN** the release title SHALL include the validated year from TMDB
