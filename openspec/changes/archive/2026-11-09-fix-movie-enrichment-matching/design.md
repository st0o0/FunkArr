# Design: Movie Enrichment Prefix Matching

## Approach

Add a candidate title normalization step in `MovieEnricher.ResolveCandidate` that
strips common Mediathek suffixes before comparing against TMDB titles.

## Title Normalization

German public broadcasters append metadata suffixes to movie titles:

| Mediathek Title | Stripped |
|---|---|
| `Schachnovelle - Spielfilm, Deutschland/Österreich 2020` | `Schachnovelle` |
| `Domino - Spielfilm USA 2023` | `Domino` |
| `Emily (Spielfilm)` | `Emily` |
| `Code 3` | `Code 3` (no suffix) |

Normalization rules (applied in order):
1. Remove parenthesized suffixes: strip ` (...)` at end
2. Split on ` - ` and take the first segment
3. Trim whitespace

## Matching Strategy

```
ResolveCandidate(candidate, movie, altTitles):

  1. normalizedCandidate = StripSuffix(candidate.Title)
  2. For each tmdbTitle in [movie.Title, movie.OriginalTitle, ...altTitles]:
     a. Levenshtein similarity (existing, unchanged)
     b. NEW: if Normalize(normalizedCandidate) == Normalize(tmdbTitle)
        → prefixConfidence = 0.95
  3. Pick best: max(levenshteinBest, prefixConfidence)
  4. If best >= threshold → return EnrichedMovie
```

The prefix match uses the same `Normalize()` as Levenshtein (lowercase, umlaut
expansion, ß→ss) to handle case and encoding differences.

## Confidence Value

Prefix match confidence = **0.95** (not 1.0, because the suffix could theoretically
indicate a different work with the same title). This is above the default threshold
(0.5) and the configured threshold for most rulesets (0.7).

## Edge Cases

- **Title IS the suffix** (e.g. `"Spielfilm"`): The strip produces `"Spielfilm"` which
  won't match any TMDB movie title. Safe.
- **Multiple ` - ` separators** (e.g. `"Title - Part 2 - Spielfilm"`): Split on first
  ` - ` produces `"Title"`, which may lose "Part 2". Acceptable — the Levenshtein path
  still catches `"Title - Part 2"` if similarity is high enough.
- **No suffix**: `StripSuffix` returns the original title. The prefix comparison then
  acts as an exact-match check (same as Levenshtein 1.0 but cheaper).

## Files Changed

| File | Change |
|---|---|
| `FunkArr.Enrichment/Tmdb/MovieEnricher.cs` | Add `StripSuffix`, use in `ResolveCandidate` |
| `FunkArr.Enrichment.Tests/Tmdb/MovieEnricherTests.cs` | Test suffix stripping + prefix match cases |
