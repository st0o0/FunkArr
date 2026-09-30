## Why

Specialized Newznab searches (`t=movie`, `t=tvsearch`) return 0 results whenever no RuleSet rules exist for the requested show or movie. The Mediathek items are fetched successfully but discarded at the `rules.Count == 0` gate in `ShowActor.HandleMatch` and `MovieActor.HandleMatch`. This makes FunkArr unusable as a movie indexer — Radarr validates with a bare `t=movie` query and rejects the indexer when it gets 0 results. Only `t=search` (text search) works because it bypasses rule matching entirely.

## What Changes

- **No-rules fallback in ShowActor and MovieActor**: When `rules.Count == 0` after auto-generation attempt, return fetched items through ContentFilter + QualityExpander instead of returning an empty list. Rules become a precision enhancement, not a prerequisite gate.
- **Bare movie search via topic filter**: `t=movie` without `q` or `imdbid` returns recent movies by querying Mediathek with `ByTopic("Filme")`, using the broadcasters' own topic-based categorization ("Filme", "Filme in der ARD", "Kino - Filme").
- **Bare TV search via latest entries**: `t=tvsearch` without `q` or `tvdbid` returns recent content (similar to `t=search` bare behavior) instead of producing an empty response from a null-state ShowActor.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `movie-actor`: HandleMatch returns fallback results (ContentFilter + QualityExpander) when no rules exist instead of empty list
- `show-actor`: HandleMatch returns fallback results (ContentFilter + QualityExpander) when no rules exist instead of empty list
- `movie-search-pipeline`: Bare movie search (`t=movie` without params) produces results via `ByTopic("Filme")` topic query
- `tv-search-pipeline`: Bare TV search (`t=tvsearch` without params) produces results instead of empty response
- `search-request-actor`: Handles bare movie/TV search cases before dispatching to actor pipeline

## Impact

- **Newznab API**: `t=movie` and `t=tvsearch` will return results where they previously returned 0. No breaking contract changes — same XML response format, just populated instead of empty.
- **Radarr/Sonarr compatibility**: Radarr can add FunkArr as indexer (bare `t=movie` validation passes). Sonarr `t=tvsearch` queries succeed without pre-existing rules.
- **Affected actors**: `ShowActor`, `MovieActor`, `SearchRequestActor`.
- **Rule infrastructure**: Unchanged. Rules still take priority when they exist. Auto-generation still runs. Fallback only activates when no rules exist and auto-generation produces nothing.
