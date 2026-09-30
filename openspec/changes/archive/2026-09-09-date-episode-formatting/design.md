## Context

`ReleaseTitleBuilder.AppendTvIdentifier()` currently handles three cases:
1. Season + Episode -> `S01E05` (works with Sonarr)
2. Episode only -> `E312` (Sonarr struggles)
3. AiredAt only -> `2024-09-20` (works for Sonarr Daily type if positioned correctly)

The title format is: `Topic.{identifier}.Title.GERMAN.{quality}.WEB.h264-FunkArr`

Sonarr's Daily series type expects: `Series.Name.YYYY-MM-DD.Episode.Title...`
Sonarr's Standard type expects: `Series.Name.S##E##.Episode.Title...`

The current format already places the identifier between topic and title, which is where Sonarr expects it. The main issues are:
- Episode-only without season: `E312` is non-standard
- Some shows have no metadata at all but could use the Mediathek timestamp

## Goals / Non-Goals

**Goals:**
- Sonarr can parse episode identification for all three fallback patterns
- Maintain backward compatibility with the primary S##E## path

**Non-Goals:**
- Fixing shows that have no TVDB episode data at all (those need better rulesets or TVDB contributions)
- Changing how episode resolution works (that's upstream of title building)

## Decisions

### Decision: Map episode-only to S01E### for Sonarr compatibility

When only an episode number is available (no season), use `S01E{padded}` instead of just `E{padded}`. Sonarr's Standard type interprets absolute episode numbers as Season 1 by convention. This matches how anime indexers handle absolute numbering.

### Decision: Keep date format as YYYY-MM-DD between topic and title

The current date positioning is already Sonarr-compatible for Daily series type. No change needed for date handling itself - the issue is that some series in Sonarr aren't configured as "Daily" type. This is a Sonarr configuration issue, not a FunkArr formatting issue. Document this in the Setup Guide.

### Decision: Use Mediathek timestamp as date fallback when no metadata

When `MetadataSpec` is null but the `SearchResultItem.AiredAt` is available, pass it through to the title builder. This gives otherwise unidentified episodes at least a date for Sonarr Daily matching.

## Risks / Trade-offs

**[Trade-off] S01E### may conflict with actual Season 1** -> For shows with real seasons, an absolute episode mapped to S01 would conflict. Mitigation: this only applies when season is null (no resolution produced a season), so it's the best-effort fallback.
