## MODIFIED Requirements

### Requirement: MovieResolver resolves candidates against TMDB data
The MovieResolver SHALL be a sealed class that takes TmdbMovie data, alternative titles, MovieCandidate[], and `EnrichmentConfig`, and returns EnrichedMovie[]. It SHALL attempt to validate each candidate against the TMDB movie using title similarity and year matching. The Method field SHALL use the MatchMethod enum. If `EnrichmentConfig.Enabled` is false, the resolver SHALL return an empty array.

#### Scenario: Title match with high confidence
- **WHEN** a MovieCandidate title closely matches the TMDB title with similarity >= EnrichmentConfig.Title.Threshold (default 0.5) and the year validates
- **THEN** the EnrichedMovie SHALL have Method=MatchMethod.TitleMatch and Confidence equal to the similarity score

#### Scenario: Year match with weak title
- **WHEN** a MovieCandidate has similarity >= 0.3 but below the title threshold, and the year matches exactly
- **THEN** the EnrichedMovie SHALL have Method=MatchMethod.YearMatch and Confidence = similarity * 0.8

#### Scenario: Custom title threshold
- **WHEN** EnrichmentConfig.Title.Threshold is 0.4
- **THEN** a title match with similarity 0.45 SHALL be accepted (would be rejected at default 0.5)

#### Scenario: No TMDB data available
- **WHEN** TMDB lookup returns null (no movie found)
- **THEN** the candidate SHALL remain unenriched

#### Scenario: Enrichment disabled
- **WHEN** EnrichmentConfig.Enabled is false
- **THEN** the resolver SHALL return an empty array

### Requirement: MovieResolver validates year
When enriching a movie candidate with an AiredAt timestamp, the MovieResolver SHALL compare the year from the candidate against the TMDB release year. A tolerance of +/- `EnrichmentConfig.Year.Tolerance` years (default 1) SHALL be applied.

#### Scenario: Year matches
- **WHEN** candidate AiredAt year is 2024 and TMDB release year is 2024
- **THEN** the year validation SHALL pass

#### Scenario: Year within tolerance
- **WHEN** candidate AiredAt year is 2025 and TMDB release year is 2024 and Year.Tolerance is 1
- **THEN** the year validation SHALL pass

#### Scenario: Custom year tolerance
- **WHEN** EnrichmentConfig.Year.Tolerance is 3 and candidate year is 2026 and TMDB year is 2024
- **THEN** the year validation SHALL pass (|2026-2024| = 2 <= 3)

#### Scenario: Year mismatch
- **WHEN** candidate AiredAt year is 2026 and TMDB release year is 1981 and Year.Tolerance is 1
- **THEN** the year validation SHALL fail
