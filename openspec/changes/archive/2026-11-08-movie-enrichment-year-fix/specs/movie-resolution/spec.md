# Movie Resolution (Delta)

## MODIFIED Requirements

### Requirement: MovieResolver resolves candidates against TMDB data
The MovieResolver SHALL be a sealed class that takes TmdbMovie data, alternative titles, MovieCandidate[], and `EnrichmentConfig`, and returns EnrichedMovie[]. It SHALL attempt to validate each candidate against the TMDB movie using title similarity only. Year validation SHALL NOT be performed — the broadcast date from Mediathek is unrelated to the TMDB production year for re-broadcast movies. The Method field SHALL use the MatchMethod enum. If `EnrichmentConfig.Enabled` is false, the resolver SHALL return an empty array.

#### Scenario: Title match with high confidence
- **WHEN** a MovieCandidate title closely matches the TMDB title with similarity >= EnrichmentConfig.Title.Threshold (default 0.5)
- **THEN** the EnrichedMovie SHALL have Method=MatchMethod.TitleMatch and Confidence equal to the similarity score, regardless of the gap between AiredAt year and TMDB release year

#### Scenario: Re-broadcast movie with large year gap
- **WHEN** a MovieCandidate has AiredAt year 2026 and TMDB release year is 2021, and title similarity exceeds threshold
- **THEN** the EnrichedMovie SHALL be returned with Year=2021 (the TMDB production year)

#### Scenario: Custom title threshold
- **WHEN** EnrichmentConfig.Title.Threshold is 0.4
- **THEN** a title match with similarity 0.45 SHALL be accepted (would be rejected at default 0.5)

#### Scenario: No TMDB data available
- **WHEN** TMDB lookup returns null (no movie found)
- **THEN** the candidate SHALL remain unenriched

#### Scenario: Enrichment disabled
- **WHEN** EnrichmentConfig.Enabled is false
- **THEN** the resolver SHALL return an empty array

## REMOVED Requirements

### Requirement: MovieResolver validates year
**Reason**: The year validation compared the Mediathek broadcast date (AiredAt) against the TMDB production year. These are unrelated dates for movies — a 2021 film re-broadcast in 2026 is normal for public broadcasters. Title similarity is a sufficient guard against false matches.
**Migration**: No migration needed. Year tolerance config remains in the schema but is no longer applied for movie enrichment.
