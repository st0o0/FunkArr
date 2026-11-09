# Fix Movie Enrichment Title Matching

## Problem

Movie enrichment fails for candidates whose Mediathek title contains the TMDB title
as a prefix but has a long suffix that tanks the Levenshtein similarity score.

Example: TMDB title `"Schachnovelle"` vs Mediathek candidate
`"Schachnovelle - Spielfilm, Deutschland/Österreich 2020"`. The Levenshtein similarity
is ~0.26 (14 / 54 chars) — well below the default threshold of 0.5.

When enrichment fails, `MovieIdentity.Year` stays null, and `SceneRelease.ForMovie`
falls back to `AiredAt.Year` (2026 broadcast year) instead of the TMDB release year
(2021). Radarr rejects all results due to year mismatch → 0 reports downloaded.

## Root Cause

`MovieEnricher.ResolveCandidate` uses only Levenshtein similarity to match candidate
titles against TMDB titles. This penalizes candidates with descriptive suffixes common
in German public broadcaster metadata (e.g. `" - Spielfilm, Deutschland 2020"`,
`" (Kurzfilm)"`, `" - Drama, USA 2019"`).

## Solution

Add a **normalized prefix check** before the Levenshtein comparison. If the candidate
title (after stripping parentheticals and ` - ` suffixes) starts with the TMDB title
(case-insensitive, umlaut-normalized), treat it as a high-confidence match.

```
"Schachnovelle - Spielfilm, Deutschland/Österreich 2020"
        │
        ▼ strip " - ..." suffix
"Schachnovelle"
        │
        ▼ compare to TMDB title "Schachnovelle"
exact match → confidence 0.95
```

This is safe because:
- The prefix check runs after scoring — only matched candidates reach enrichment
- The TMDB title is the authoritative name from an external database
- German broadcasters consistently use `"Title - Genre, Country Year"` format

## Scope

- `MovieEnricher.ResolveCandidate` in `FunkArr.Enrichment/Tmdb/MovieEnricher.cs`
- Tests in `FunkArr.Enrichment.Tests/Tmdb/MovieEnricherTests.cs`
- No API, message, or persistence changes
