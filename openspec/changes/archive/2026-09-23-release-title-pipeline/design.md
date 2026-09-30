## Context

Today's release title flow mixes decision-making with formatting in a single `ReleaseTitleBuilder.Build` call:

```
ReleaseVariant.Expand(item, mediaType, mediaName)
  → MetadataSpec(season, episode, airedAt)
  → ReleaseTitleBuilder.Build(mediaName, Source.Title, metadata, quality, mediaType)
      → StripTopicFromTitle(mediaName, rawTitle)  ← decision
      → AppendTvIdentifier(metadata)               ← formatting
      → StripSeasonEpisodePattern(sanitized)        ← decision (cleanup)
      → Sanitize + CollapseDots                     ← formatting
```

The raw Mediathek title passes through the full pipeline carrying topic names, S##E## tags, "Staffel" metadata, and accessibility suffixes that must be stripped heuristically. Meanwhile, the pipeline already produces clean episode titles — Scoring's `ConstructedTitle` and Enrichment's TVDB `EpisodeName` — but both are discarded in the worker state's `Apply` methods and never reach the title builder.

## Goals / Non-Goals

**Goals:**
- Clean separation: one component decides WHAT the title is, another formats HOW it looks
- Each pipeline phase contributes its knowledge to a shared display record
- `ReleaseTitleBuilder` becomes a pure formatter with no decision logic
- Eliminate all subtractive heuristics (StripTopicFromTitle, S##E## regex stripping)
- Fix all 25 identified naming issues across 69 rulesets

**Non-Goals:**
- Changing the scene-style output format (Sonarr/Radarr compatibility preserved)
- Changing the scoring or enrichment algorithms
- Per-item ruleset resolution for movies (shared-topic problem deferred)
- Changing MetadataSpec's role in scoring rule output (it stays as-is for the scoring engine)

## Decisions

### Decision 1: ReleaseDisplay record in FunkArr.Search

```csharp
record ReleaseDisplay(string MediaName, string EpisodeTitle);
```

Two fields, each with a clear responsibility:
- `MediaName`: the series/film name Sonarr uses for recognition ("Tatort", "Leschs Kosmos")
- `EpisodeTitle`: the episode-specific part, already clean — no topic, no S##E##, no "Staffel"

**Why not in FunkArr.Messages?** `ReleaseDisplay` is a search-pipeline concept, not a cross-domain message. It's consumed only within the search flow (worker states → ReleaseVariant → ReleaseTitleBuilder). Keeping it in FunkArr.Search avoids leaking display concerns into the message contract.

**Why not a single `ResolvedTitle` string?** Keeping MediaName and EpisodeTitle separate lets the formatter apply different sanitization (MediaName is stable, EpisodeTitle varies per item) and produce the correct scene-style structure.

### Decision 2: EnrichedItem carries Display

```csharp
record EnrichedItem(
    int Index, SourceInfo Source, double Score,
    bool Matched, bool HasScoringMetadata,
    MediaIdentity Identity, MatchInfo? Match,
    ReleaseDisplay? Display);                    // ← new
```

`Display` is nullable because unscored items (no matching ruleset) won't have display data until the fallback phase.

### Decision 3: Three-tier EpisodeTitle resolution with clear priority

```
Priority 1: TVDB EpisodeName    (from Enrichment, confidence ≥ 0.9)
Priority 2: ConstructedTitle    (from Scoring TitleRules/Regex)
Priority 3: CleanTitle fallback (Source.Title with topic stripped)
```

**Why confidence threshold on TVDB?** Low-confidence enrichment matches (AirDate-only) may have wrong episode names. Using the TVDB name only at high confidence prevents displaying a wrong title. Below the threshold, the scoring-extracted title is used (which is closer to the Mediathek content).

**Where does resolution happen?** In the worker state `Apply` methods — the same place that already sets Identity. Each Apply sets what it can:

```
ApplyRuleSet(...)           → Display = new(mediaName, "")
Apply(ScoreCompleted)       → Display = Display with { EpisodeTitle = constructedTitle }
Apply(EnrichCompleted)      → if confidence ≥ 0.9: Display = Display with { EpisodeTitle = tvdbName }
ToSearchCompleted()         → items without Display get fallback
```

### Decision 4: ReleaseTitleBuilder.Format replaces Build

```csharp
static string Format(string mediaName, string? identifier, string episodeTitle, int quality)
```

Parameters are all pre-resolved — no raw Mediathek data enters the formatter. The method:
1. Sanitizes mediaName
2. Appends identifier (S##E##, date, or nothing)
3. Sanitizes episodeTitle
4. Appends GERMAN + quality + release group

Removed entirely: `StripTopicFromTitle`, `StripSeasonEpisodePattern`, `AppendTvIdentifier` (merged into caller), `AppendMovieIdentifier` (merged into caller).

Retained: `Sanitize`, `CollapseDots`, `MapQuality`, `PadNumber`, `FormatSeasonEpisode` (these are pure formatting utilities).

### Decision 5: Identifier built in ReleaseVariant.Expand

The S##E## / date / year identifier is built in `ReleaseVariant.Expand` from `MediaIdentity` — not in the builder. This is where the data is, and it avoids passing metadata records into the formatter.

```csharp
var identifier = (identity.Season, identity.Episode, source.AiredAt, mediaType) switch
{
    ({ } s, { } e, _, MediaType.Show)  => FormatSeasonEpisode(s, e),
    (null, { } e, _, MediaType.Show)   => $"S01E{PadNumber(e)}",
    (_, _, { } at, MediaType.Show)     => at.ToString("yyyy-MM-dd"),
    (_, _, { } at, MediaType.Movie)    => at.Year.ToString(),
    _                                  => null,
};
```

### Decision 6: CleanTitle as isolated fallback

For items without ConstructedTitle or TVDB match (unscored items, failed enrichment), a `CleanTitle` function strips the topic from the raw Mediathek title. This is the only place topic-stripping logic remains — isolated, clearly marked as a fallback, not the main path.

`CleanTitle` uses the existing prefix/suffix stripping logic from `StripTopicFromTitle` but lives in `ReleaseVariant` (or a small helper in FunkArr.Search), not in the builder.

## Risks / Trade-offs

**ConstructedTitle removal from MetadataSpec** — `MetadataSpec.ConstructedTitle` was added recently to carry scoring-extracted titles through to enrichment (for TVDB matching). Removing it from MetadataSpec means the worker state must track it separately. This is already how it works today (`_constructedTitles` dictionary in the worker state), so MetadataSpec.ConstructedTitle was always redundant transit.

**Confidence threshold tuning** — Using TVDB EpisodeName only at ≥ 0.9 confidence may miss correct low-confidence matches whose episode name would be better than the scoring title. This is a safe default; it can be tuned per-ruleset if needed.

**Movie mediaName unchanged** — The shared-topic movie problem (all films under "Spielfilm" get the same mediaName) is not addressed. It requires per-item ruleset resolution which is a larger architectural change.
