## Context

The Newznab indexer API produces release titles that are indistinguishable for multi-episode shows (e.g. multiple "Tatort.GERMAN.720p.WEB.h264-FA" entries). The RuleSet matching engine already resolves episode metadata (season, episode number, episode name, air date) from TVDB during TV search, but this data is discarded at two points:

1. `ShowActor.HandleMatch` converts `MatchedEpisodeInfo` to `MatchedItemInfo(item, matchedTitle)` — losing structured season/episode data
2. `SearchRequestActor.HandleMatchComplete` extracts only `m.Item` from `MatchedItemInfo` — losing even the episode name string

The XML response is built with imperative `XmlWriter` calls, making it verbose to maintain. MediathekArr demonstrates a cleaner approach using `XmlSerializer` with declarative model classes.

## Goals / Non-Goals

**Goals:**
- Release titles include episode names and resolved S/E numbers from RuleSet matching
- Daily-style shows get `YYYY.MM.DD` format titles (parseable by Sonarr)
- Text search / browse results include episode title + air date as fallback differentiation
- XML generation uses declarative serialization models (XmlSerializer)
- Eliminate `NewznabResult` intermediate DTO

**Non-Goals:**
- EpisodeType detection logic (daily vs standard) — use air date presence as heuristic
- Movie matching improvements
- Changes to FakeNzbBuilder
- Changes to download pipeline or SABnzbd API

## Decisions

### 1. XmlSerializer with mutable DTOs for XML models

**Choice**: Use `System.Xml.Serialization.XmlSerializer` with `{ get; set; }` classes (not records with `init`).

**Why**: `XmlSerializer` requires a parameterless constructor and settable properties. Using `sealed class` with `[Xml*]` attributes is the idiomatic approach. These are pure serialization DTOs with no behavior, so the mutable-class exception is justified.

**Alternative considered**: Keep `XmlWriter` and just add title-building logic. Rejected because the imperative approach is already hard to read at 100+ lines, and adding more attributes/fields makes it worse.

### 2. Enrich SearchResult rather than introduce a new response type

**Choice**: Add optional properties to `SearchResult` (`int? ResolvedSeason`, `int? ResolvedEpisode`, `string? EpisodeName`, `string? AirDate`, `string? ResolvedShowName`).

**Why**: Minimal pipeline disruption. `SearchResult` is already the type flowing through `QualityExpander` and `ResultScorer`. Adding nullable fields doesn't break existing consumers. The alternative (a wrapper type or parallel list) would require changes to every pipeline step's signature.

**Alternative considered**: `EnrichedSearchResponse` with `(SearchResult, MatchedEpisodeInfo?)` tuples. Rejected because it would require `QualityExpander` and `ResultScorer` to understand the tuple, and `MatchedEpisodeInfo` lives in the `RuleSet` namespace creating a cross-cutting dependency.

### 3. MatchedItemInfo carries MatchedEpisodeInfo directly

**Choice**: Change `MatchedItemInfo` from `(MediathekResultItem Item, string MatchedTitle)` to `(MediathekResultItem Item, MatchedEpisodeInfo? EpisodeInfo)`.

**Why**: The `MatchedTitle` string is lossy — it carries "S02E05" or an episode name but not both, and not the structured data needed for title building. Passing the full `MatchedEpisodeInfo` (which contains `TvdbEpisodeInfo` with season, episode, name, air date) preserves everything.

### 4. Controller builds RssItem directly from SearchResult

**Choice**: Eliminate `NewznabResult` as an intermediate type. The controller maps `SearchResult` → `RssItem` directly using a `NewznabResultMapper` static class.

**Why**: `NewznabResult` was a bridge between `SearchResult` (which lacked episode data) and the XML builder. Once `SearchResult` carries episode info, the bridge is redundant.

### 5. ReleaseTitleBuilder with four variants

**Choice**: Static class with methods:
- `BuildStandard(showName, season, episode, episodeName, quality, codec)` → `Show.S01E03.Episode.Name.GERMAN.720p.WEB.h264-FA`
- `BuildDaily(showName, airDate, episodeName, quality, codec)` → `Show.2026.08.17.Episode.Name.GERMAN.720p.WEB.h264-FA`
- `BuildMovie(movieName, year, quality, codec)` → `Movie.2024.GERMAN.720p.WEB.h264-FA`
- `BuildFallback(topic, title, timestamp, quality, codec)` → `Topic.Title.2026.08.17.GERMAN.720p.WEB.h264-FA`

**Heuristic for daily vs standard**: If `ResolvedSeason` is present and > 0, use standard. If only `AirDate` is present (or season=0/1 with high episode numbers typical of daily shows), use daily format.

### 6. Fallback for unmatched results (text search / browse)

**Choice**: When `SearchResult` has no resolved episode data (text search path, no RuleSet matching), use the raw `Title` field (Mediathek episode title) and `Timestamp` (air date) to build a differentiated filename.

**Why**: Even without TVDB resolution, MediathekViewWeb provides episode titles like "Köpfe" and timestamps. Including these in the release name makes results distinguishable in Prowlarr.

## Risks / Trade-offs

- **[XmlSerializer startup cost]** → First serialization incurs JIT cost for the generated serializer assembly. Mitigated: cache the `XmlSerializer` instance as a static field. Subsequent calls are fast.
- **[Episode name sanitization]** → Episode names may contain special characters (umlauts, punctuation). Mitigated: sanitize with same `Replace(' ', '.')` + strip non-filename-safe characters.
- **[Release title length]** → Adding episode names makes titles longer, potentially exceeding display limits in some UIs. Mitigated: truncate episode name portion at 40 characters.
- **[Contract test breakage]** → Existing `.verified.txt` snapshot files will differ due to cosmetic XML formatting differences between XmlWriter and XmlSerializer. Mitigated: regenerate all verified files. The XML structure is semantically identical.
- **[Fallback quality]** → Raw Mediathek titles may contain garbage like "(mit Untertiteln)" or "Folge 5". Mitigated: apply basic sanitization (strip parenthetical suffixes, common noise words).
