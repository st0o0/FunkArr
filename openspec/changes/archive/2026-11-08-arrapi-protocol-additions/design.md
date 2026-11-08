## Context

`IndexerRequest` currently accepts 11 params. Prowlarr sends `maxage`, `minsize`, `maxsize`, and `title` on searches but FunkArr doesn't bind them — they're silently ignored. `NewznabAttributeBuilder` emits 7 attribute types but not `language`, `subs`, or `usenetdate`. Daily shows send `ep=MM/DD` which `ParseInt` returns null for, dropping the episode filter silently.

## Goals / Non-Goals

**Goals:**
- Accept Prowlarr filter params without error (bind, don't forward)
- Emit `language`, `subs`, `usenetdate` attributes for better Arr Custom Format support
- Parse date-based `ep` values for daily shows
- Complete the Newznab error code catalog

**Non-Goals:**
- Server-side filtering by maxage/minsize/maxsize (FunkArr returns all results; Arr filters client-side)
- `comments` attribute (needs SourceInfo threading — separate change)
- `usenetdate` on search-backed items (only meaningful for feed items with FirstSeenAt)

## Decisions

### 1. Filter params: accept but don't forward

Add `Title`, `MaxAge`, `MinSize`, `MaxSize` to `IndexerRequest` as optional string/int/long params. They bind from query string but are not passed to `SearchCommand`. This prevents Prowlarr from logging "unsupported parameter" warnings.

**Why not filter?** FunkArr's search returns pre-scored, evaluated results. Filtering by age/size at the Newznab layer would discard valid matches. Prowlarr already applies these filters client-side after receiving results.

### 2. Caps supportedParams update

Add the new params to each search type's `supportedParams` in `Caps.cs`:
- `search`: `"q,title,maxage,minsize,maxsize"`
- `tv-search`: `"q,season,ep,tvdbid,imdbid,tmdbid,title,maxage,minsize,maxsize"`
- `movie-search`: `"q,imdbid,tmdbid,title,maxage,minsize,maxsize"`

### 3. Item attributes via NewznabAttributeBuilder

Add to `BuildAttributes`:
- `language` = `"German"` — always emitted (all Mediathek content is German)
- `subs` = `"German"` — emitted when `item.SubtitleUrl` is not null/empty

For `usenetdate`: add an optional `DateTimeOffset? firstSeenAt` parameter to `BuildAttributes`. `RssItemBuilder.ToItem` passes it through from the feed entry's `FirstSeenAt`. For search-backed items it's null and the attribute is omitted.

### 4. Daily show ep=MM/DD parsing

Add a `ParseEpisode` method alongside `ParseInt` that handles both formats:
- Integer (`"17"`) → returns `17` (existing behavior)
- Date (`"11/08"` or `"1108"`) → returns the integer representation (e.g., `1108`)

The key insight: Sonarr sends daily show episodes as `MMDD` (no slash) — e.g., `ep=1108` for November 8th. This already parses as integer `1108`. The season is the year (`season=2026`). So the actual fix is: the `FilterBySeasonEpisode` comparison `int.TryParse(i.Metadata?.Episode, out var e) && e == episode` needs the enriched episode metadata to also be in MMDD format. This is an enrichment concern, not a parsing concern.

**Revised approach:** The `ep` parameter already parses fine for the `MMDD` format that Sonarr actually sends. The real issue is that enriched `Metadata.Episode` stores episode numbers like `"17"` (episode-of-season), not `"1108"` (MMDD). For daily shows, enrichment would need to produce MMDD-format episode values. This is an enrichment domain change, not an ArrApi change.

**Decision:** Skip daily show parsing for now — it's an enrichment problem, not a protocol problem. Document in the backlog that daily show support requires enrichment changes.

### 5. Error codes

Add static fields to `NewznabError`:
- `101` Account Suspended
- `102` Insufficient Privileges  
- `103` Registration Denied
- `104` Registrations Closed
- `105`-`109` reserved (single "Reserved" entry, not 5 separate)
- `203` Function Not Available

These are never triggered by FunkArr (no accounts, no registration) but exist for protocol completeness — Prowlarr's error parser expects the full catalog.

## Risks / Trade-offs

- **[Filter params ignored]** → Prowlarr may show results the user tried to filter out (by age/size). Mitigation: this is already the case; adding caps params just stops the "unsupported" warning.
- **[Daily show deferred]** → Daily shows still won't filter correctly by episode. Mitigation: they work for RSS sync (no episode filter applied); only manual Sonarr episode searches are affected.
