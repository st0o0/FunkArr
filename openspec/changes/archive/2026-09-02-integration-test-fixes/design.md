## Context

First integration test with Prowlarr/Sonarr/Radarr revealed two issues: categories show as "Unknown" in Prowlarr because the Newznab caps response declares no categories and all results use TV category codes regardless of search type, and entries from broadcasters that don't report file sizes (notably ORF) show as "0 B".

The Newznab caps categories requirement already exists in the spec but the implementation has an empty categories list. The category attribute in search results is hardcoded to TV categories (5040/5030) regardless of whether the search was triggered via `t=movie` or `t=tvsearch`.

## Goals / Non-Goals

**Goals:**
- Prowlarr recognizes FunkArr results with correct category labels (TV HD, TV SD, Movies HD, Movies SD)
- Caps response declares the supported categories so Prowlarr can filter and sort
- Entries with unknown size show a reasonable estimate instead of 0 B
- Keep changes minimal — adapter-level fixes only, no domain changes

**Non-Goals:**
- Subcategory mapping beyond HD/SD (no 4K, no documentary vs drama distinction)
- Accurate size from HTTP HEAD probes (too slow for search results)
- Changing the search pipeline or message types

## Decisions

### Decision 1: Pass search type as parameter to ToRss

The `ToRss` method currently has no knowledge of whether the search was TV or movie. Rather than adding a field to `SearchResultItem` (which would leak adapter concerns into the domain), pass the search type as a parameter to `ToRss` and `BuildAttributes` from the endpoint handler. The handler already knows the `t` parameter value.

**Alternative considered:** Add `SearchType` to `SearchResultItem` — rejected because category mapping is a Newznab adapter concern, not a domain concern. The domain doesn't know or care about Newznab category IDs.

### Decision 2: Estimate size from duration × bitrate

When `MediathekItem.Size` is 0 (already defaulted from null at the API boundary), estimate based on the quality tier and duration:

| Quality | Bitrate estimate | Formula |
|---------|-----------------|---------|
| HD (1080p) | ~2.5 Mbps | `duration × 312500` |
| SD (720p) | ~1.5 Mbps | `duration × 187500` |
| Low (360p) | ~0.8 Mbps | `duration × 100000` |

Place the estimation in `MediathekViewWebManager` at the mapping boundary (where null is already handled), so the rest of the pipeline always sees a non-zero size.

**Alternative considered:** Estimate in the ArrApi adapter — rejected because 0-size results would propagate through the internal API too. Better to fix at the source.

### Decision 3: Populate Caps categories statically

The categories are fixed (TV + Movies, HD + SD each). No need for dynamic generation — a static initializer in the `Caps` model is sufficient.

## Risks / Trade-offs

- **Size estimates are approximations** → Acceptable. Sonarr/Radarr use size mainly for quality profile matching, not storage planning. An estimate in the right ballpark is far better than 0 B.
- **All results from `t=search` (general) use TV categories** → We use the `cat` parameter or default to TV for general search. This matches how most Newznab indexers behave since FunkArr content is primarily TV.
