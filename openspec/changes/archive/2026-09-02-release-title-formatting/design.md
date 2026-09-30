## Context

FunkArr's scoring system (MatchMagic) already extracts season, episode, and airdate from Mediathek items using ruleset-defined strategies (RegexCapture, TitleConstruction, AirdateExtraction, ByAbsoluteEpisodeNumber). However, `ScoredItem` only returns `(Index, Score, Matched)` — the identification results are discarded after matching. Downstream consumers (Search workers, Newznab RSS, Download pipeline) never see what was extracted.

The *arr stack (Sonarr/Radarr) identifies media by parsing scene-style release titles like `Tatort.S01E05.Der.letzte.Schrei.GERMAN.720p.WEB.h264-FunkArr`. Currently, FunkArr passes the raw Mediathek title ("Tatort - Der letzte Schrei") as both the Newznab RSS title and the download filename, which Sonarr cannot reliably parse for import.

MediathekArr uses the same approach: scene-style titles with S/E numbering, date-based fallback for daily shows, and a `-MEDiATHEK` release group tag. FunkArr will follow the same pattern with `-FunkArr`.

## Goals / Non-Goals

**Goals:**
- Surface identification metadata (season, episode, airdate) from scoring into search results
- Build scene-style release titles that Sonarr/Radarr can parse
- Use the same formatted title for Newznab RSS items and download filenames
- Pass category through NZB metadata so downloads know tv vs movie

**Non-Goals:**
- TVDB/TMDB lookup for episode numbering — that's a future capability. For now, S/E comes from ruleset regex extraction only.
- Configurable filename patterns — one format, hardcoded
- Renaming already-downloaded files

## Decisions

### D1: MetadataSpec as a flat record in Messages

`MetadataSpec` carries the three identification outputs:

```csharp
public sealed record MetadataSpec(
    string? Season,
    string? Episode,
    DateTimeOffset? AiredAt);
```

Lives in `FunkArr.Messages.Scoring` alongside the other scoring types. Nullable fields because not every strategy produces all three. Season/Episode are strings (not ints) because they may be zero-padded ("01") or year-based ("2024").

**Why not nested in ScoredItem?** MetadataSpec is reused in SearchResultItem and could be useful in other contexts. A standalone record is more composable.

### D2: ScoredItem carries MetadataSpec

```csharp
public sealed record ScoredItem(
    int Index,
    double Score,
    bool Matched,
    MetadataSpec? Metadata);
```

Null when the item didn't match any rule or identification failed. The MatchMagicActor populates it from the identification result that's already computed during evaluation.

### D3: ReleaseTitleBuilder in Core as a pure static function

```
FunkArr.Core.ReleaseTitleBuilder.Build(
    topic, title, metadata, quality, category) → string
```

Pure function, no dependencies. Lives in Core because it's used by Search (to build RSS titles) and could be used by Download (for logging). The actual filename derivation in DownloadManager just sanitizes the title that's already scene-formatted.

Format rules:
- Spaces and special chars → dots
- Umlauts → ASCII (ä→ae, ö→oe, ü→ue, ß→ss)
- Remove chars invalid in scene titles: `/:;"'@#?$%^*+=!<>,()`
- Collapse consecutive dots
- Quality: map int → "1080p" / "720p" / "480p" / "270p"
- Category: "tv" uses S/E or date, "movie" uses year only

**Title variants by available metadata:**

| Category | Has S/E | Has AiredAt | Format |
|----------|---------|-------------|--------|
| tv | yes | * | `{Topic}.S{Season}E{Episode}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` |
| tv | no | yes | `{Topic}.{yyyy-MM-dd}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` |
| tv | no | no | `{Topic}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` |
| movie | * | yes | `{Topic}.{yyyy}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` |
| movie | * | no | `{Topic}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` |

### D4: Search workers build scene title after scoring

TvSearchWorker and MovieSearchWorker already receive `ScoreCompleted` and map results to `SearchResultItem`. The title construction happens in `ToResultItem()` — after scoring, before returning to the SearchManager. This is the natural insertion point for `ReleaseTitleBuilder.Build()`.

`SearchResultItem.Title` becomes the scene-style title. `Topic` remains as a separate field (already exists) for display purposes.

### D5: NZB meta gains X-FunkArr-Category

The NZB payload already carries `X-FunkArr-Channel`, `X-FunkArr-Duration`, `X-FunkArr-Size`, and optionally `X-FunkArr-SubtitleUrl`. Adding `X-FunkArr-Category` ("tv"/"movie") lets the download pipeline know the media type without re-deriving it. The title in the NZB is already the scene-style title.

### D6: DownloadManager uses scene title directly for OutputPath

Currently: `SanitizeFilename(cmd.Title) + ".mkv"`

New: The title is already scene-formatted with dots (no filesystem-invalid chars). `DownloadManager` just appends `.mkv`:

```
Path.Combine(_downloadPath, cmd.Title + ".mkv")
```

The `SanitizeFilename` method becomes unnecessary since `ReleaseTitleBuilder` already strips invalid chars and replaces spaces with dots.

## Risks / Trade-offs

- **[Unmatched items get degraded titles]** → Items that don't match any rule get no MetadataSpec. Their titles fall back to `{Topic}.{Title}.GERMAN.{Quality}.WEB.h264-FunkArr` — still scene-style but without S/E. Sonarr may not auto-import these. This is acceptable — unmatched items are low-confidence by definition.
- **[Season/Episode strings not validated]** → The regex extraction returns whatever the pattern captures. If a ruleset has a bad regex, Season could be "abc". ReleaseTitleBuilder passes it through as-is. → Rulesets are author-controlled; bad regexes are a ruleset quality issue, not a formatter issue.
- **[Filename collisions]** → Two episodes with the same Topic, S/E, and Title produce the same filename. → Unlikely in practice; same as MediathekArr's approach. Could add download ID suffix later if needed.
- **[Breaking change to Newznab titles]** → Existing Sonarr/Radarr setups that somehow work with raw titles will see different titles. → v0.x, breaking changes are fine per project policy.
