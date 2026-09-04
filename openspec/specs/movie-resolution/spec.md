# Movie Resolution

## Purpose

Resolves movie candidates against TMDB metadata to produce enriched MovieResolved records with validated title, year, and external IDs for Radarr integration.

## Requirements

### Requirement: MovieResolver resolves candidates against TMDB data
The MovieResolver SHALL be a static class that takes TmdbMovie data and MovieCandidate[] and returns MovieResolved[]. It SHALL attempt to validate each candidate against the TMDB movie using title similarity and year matching.

#### Scenario: TMDB ID direct match
- **WHEN** a MovieCandidate is resolved against a TmdbMovie fetched by TMDB ID
- **THEN** the MovieResolved SHALL have Confidence=1.0, Strategy="TmdbIdLookup"

#### Scenario: IMDB ID resolved to TMDB
- **WHEN** a MovieCandidate is resolved via IMDB ID → TMDB find
- **THEN** the MovieResolved SHALL have Confidence=0.95, Strategy="ImdbIdLookup"

#### Scenario: No TMDB data available
- **WHEN** TMDB lookup returns null (no movie found)
- **THEN** the candidate SHALL remain unresolved

### Requirement: MovieResolver validates title similarity
When resolving a movie candidate, the MovieResolver SHALL compute Levenshtein similarity between the candidate title and the TMDB title (and alternative titles). The match SHALL be accepted only if the similarity exceeds a configurable threshold (default 0.5).

#### Scenario: Title matches TMDB title
- **WHEN** candidate title is "Das Boot" and TMDB title is "Das Boot"
- **THEN** the title validation SHALL pass with high confidence

#### Scenario: Title matches alternative title
- **WHEN** candidate title is "The Boat" and TMDB alternative title is "The Boat"
- **THEN** the title validation SHALL pass

#### Scenario: Title does not match
- **WHEN** candidate title is "Tatort Ölfeld" and TMDB title is "Der letzte Tango in Paris"
- **THEN** the title validation SHALL fail and the candidate SHALL be unresolved

### Requirement: MovieResolver validates year
When resolving a movie candidate with an AiredAt timestamp, the MovieResolver SHALL compare the year from the candidate against the TMDB release year. A tolerance of ±1 year SHALL be applied.

#### Scenario: Year matches
- **WHEN** candidate AiredAt year is 2024 and TMDB release year is 2024
- **THEN** the year validation SHALL pass

#### Scenario: Year within tolerance
- **WHEN** candidate AiredAt year is 2025 and TMDB release year is 2024
- **THEN** the year validation SHALL pass (±1 year tolerance)

#### Scenario: Year mismatch
- **WHEN** candidate AiredAt year is 2026 and TMDB release year is 1981
- **THEN** the year validation SHALL fail (likely a re-broadcast, not matching)

### Requirement: MovieResolved contains enriched metadata
Each MovieResolved record SHALL contain the validated Title, Year, ImdbId, TmdbId, Confidence, and Strategy. This metadata SHALL be used to build enriched release titles for Radarr.

#### Scenario: Fully resolved movie
- **WHEN** a movie is resolved via TMDB
- **THEN** MovieResolved SHALL have Title (TMDB title), Year (release year), ImdbId (if known), TmdbId, Confidence, Strategy
