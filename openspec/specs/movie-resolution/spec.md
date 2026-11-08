# Movie Resolution

## Purpose

Enriches movie candidates against TMDB metadata to produce EnrichedMovie records with validated title, year, and external IDs for Radarr integration.

## Requirements

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

### Requirement: MovieResolver validates title similarity
When enriching a movie candidate, the MovieResolver SHALL compute Levenshtein similarity between the candidate title and the TMDB title (and alternative titles). The match SHALL be accepted only if the similarity exceeds threshold (0.5).

#### Scenario: Title matches TMDB title
- **WHEN** candidate title is "Das Boot" and TMDB title is "Das Boot"
- **THEN** the title validation SHALL pass with high confidence

#### Scenario: Title matches alternative title
- **WHEN** candidate title is "The Boat" and TMDB alternative title is "The Boat"
- **THEN** the title validation SHALL pass

#### Scenario: Title does not match
- **WHEN** candidate title is "Tatort: Oelfeld" and TMDB title is "Der letzte Tango in Paris"
- **THEN** the title validation SHALL fail and the candidate SHALL be unenriched

### Requirement: MovieResolved contains enriched metadata
Each EnrichedMovie record SHALL contain the validated Title, Year, ImdbId, TmdbId, Confidence, and Method. This metadata SHALL be used to build enriched release titles for Radarr.

#### Scenario: Fully enriched movie
- **WHEN** a movie is enriched via TMDB
- **THEN** EnrichedMovie SHALL have Title (TMDB title), Year (release year), ImdbId (if known), TmdbId, Confidence, Method (MatchMethod enum)
