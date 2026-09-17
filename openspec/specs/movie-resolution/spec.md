# Movie Resolution

## Purpose

Enriches movie candidates against TMDB metadata to produce EnrichedMovie records with validated title, year, and external IDs for Radarr integration.

## Requirements

### Requirement: MovieResolver resolves candidates against TMDB data
The MovieResolver SHALL be a sealed class that takes TmdbMovie data, alternative titles, and MovieCandidate[] and returns EnrichedMovie[]. It SHALL attempt to validate each candidate against the TMDB movie using title similarity and year matching. The Method field SHALL use the MatchMethod enum instead of string constants.

#### Scenario: Title match with high confidence
- **WHEN** a MovieCandidate title closely matches the TMDB title with similarity >= 0.5 and the year validates
- **THEN** the EnrichedMovie SHALL have Method=MatchMethod.TitleMatch and Confidence equal to the similarity score

#### Scenario: Year match with weak title
- **WHEN** a MovieCandidate has similarity >= 0.3 but < 0.5, and the year matches exactly
- **THEN** the EnrichedMovie SHALL have Method=MatchMethod.YearMatch and Confidence = similarity * 0.8

#### Scenario: No TMDB data available
- **WHEN** TMDB lookup returns null (no movie found)
- **THEN** the candidate SHALL remain unenriched

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

### Requirement: MovieResolver validates year
When enriching a movie candidate with an AiredAt timestamp, the MovieResolver SHALL compare the year from the candidate against the TMDB release year. A tolerance of +/-1 year SHALL be applied.

#### Scenario: Year matches
- **WHEN** candidate AiredAt year is 2024 and TMDB release year is 2024
- **THEN** the year validation SHALL pass

#### Scenario: Year within tolerance
- **WHEN** candidate AiredAt year is 2025 and TMDB release year is 2024
- **THEN** the year validation SHALL pass (+/-1 year tolerance)

#### Scenario: Year mismatch
- **WHEN** candidate AiredAt year is 2026 and TMDB release year is 1981
- **THEN** the year validation SHALL fail (likely a re-broadcast, not matching)

### Requirement: MovieResolved contains enriched metadata
Each EnrichedMovie record SHALL contain the validated Title, Year, ImdbId, TmdbId, Confidence, and Method. This metadata SHALL be used to build enriched release titles for Radarr.

#### Scenario: Fully enriched movie
- **WHEN** a movie is enriched via TMDB
- **THEN** EnrichedMovie SHALL have Title (TMDB title), Year (release year), ImdbId (if known), TmdbId, Confidence, Method (MatchMethod enum)
