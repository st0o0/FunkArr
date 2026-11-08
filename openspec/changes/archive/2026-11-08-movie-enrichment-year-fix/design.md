## Context

Movie enrichment via TMDB works correctly for the TMDB lookup step but fails at candidate validation. `MovieEnricher.ValidateYear` compares the Mediathek broadcast date (`AiredAt.Year`) against the TMDB production year. For re-broadcasts (a 2021 film airing in 2026), this exceeds the default tolerance of 1 and the candidate is rejected. The production year never reaches `MovieIdentity.Year`, so `SceneRelease.ForMovie` falls back to the broadcast year — which Radarr then rejects.

A secondary issue: when the scoring engine identifies a movie title that matches the media name (e.g., both are "Schachnovelle"), `FormatTitle` produces stuttered output like `Schachnovelle.2026.Schachnovelle.GERMAN...`.

## Goals / Non-Goals

**Goals:**
- Movie enrichment succeeds for re-broadcasts regardless of the gap between broadcast and production year
- Radarr receives the correct production year in Newznab titles
- No title stuttering when episode title matches media name

**Non-Goals:**
- Changing the enrichment schema or year tolerance config (the field stays but is unused for movies)
- Modifying TV show enrichment behavior
- Changing the `YearMatchConfig` message or persistence types (they remain for backward compatibility)

## Decisions

### Remove year validation from MovieEnricher entirely

**Decision**: Remove the `ValidateYear` call and the `YearMatch` fallback path from `MovieEnricher.ResolveCandidate`. Title similarity alone guards against false matches.

**Why**: The year comparison is semantically wrong for movies. `AiredAt` is when the broadcaster scheduled the film, not when it was produced. These are unrelated dates. TV shows have `AiredAt ≈ production year` (same season), but movies routinely air decades after production.

**Alternative considered**: Increase default tolerance to 10+ years. Rejected because the comparison itself is meaningless — a high tolerance just means "always pass", so the check adds nothing.

**Impact on the `YearMatch` method**: The second match path (similarity >= 0.3, exact year match) is removed entirely. It was a weaker fallback that only triggered when title similarity was below threshold but the year happened to match — a coincidence-based match that could produce false positives for movies with common years.

### Deduplicate title in SceneRelease.ForMovie

**Decision**: In `SceneRelease.ForMovie`, after resolving display, check if `EpisodeTitle` equals `MediaName` (ordinal, case-insensitive). If so, set `EpisodeTitle` to empty string.

**Why**: For movies where the source title is just the movie name (e.g., "Schachnovelle" under topic "Spielfilm"), `ReleaseDisplay.Clean` may leave the movie name as the episode title. Combined with MediaName also being the movie name, `FormatTitle` produces `Name.Year.Name.GERMAN...`.

## Risks / Trade-offs

- **[Risk] False positive movie matches without year guard** → Title similarity threshold (default 0.5) is sufficient. Levenshtein at 0.5 requires substantial character overlap. The scoring engine already pre-filters candidates by topic/title rules before enrichment runs.
- **[Risk] Existing movie rulesets break** → No movie ruleset currently has enrichment configured. Enrichment defaults apply for all. The change makes defaults work correctly rather than silently failing.
- **[Risk] YearMatchConfig becomes dead code for movies** → Acceptable. The config type stays in Messages for schema/persistence compatibility. It's still used conceptually (tolerance is read but no longer applied). Removing it would be a separate cleanup.
