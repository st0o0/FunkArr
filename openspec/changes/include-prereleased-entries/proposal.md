# Proposal

## Why

All MediathekViewWeb queries are sent with `future=false`, which hides every entry whose broadcast time lies in the future. Broadcasters, ZDF in particular, regularly publish episodes in the Mediathek **before** the TV broadcast. These episodes can be downloaded but stay invisible to FunkArr until air time, so users get them hours or days later than possible (#205). MVW's own RSS feed and MediathekArr both query with `future=true`.

## What Changes

- New option `FunkArr:Search:IncludeFuture` (default `true`).
- TV search, movie search and the internal Mediathek search (`/api/mediathek/search`, used by the UI and the ruleset builder) send `future` according to that option.
- Documentation explains the option and how to wait for the broadcast instead (Sonarr's release-profile air date restriction).

## Capabilities

### New Capabilities

- `prereleased-entries`: whether Mediathek entries published before their broadcast time are included in searches, and how this is configured.

### Modified Capabilities

_None._ The existing query requirements in `tv-search` and `movie-search` do not state a `future` value; the new capability owns it.

## Impact

- **Core / Host:** `SearchOptions` (`FunkArr:Search`) registered in `SearchSetupContainer`.
- **Search:** worker states take the option; `TvSearchWorker` / `MovieSearchWorker` read it via `IOptionsMonitor` (optional constructor parameter, like the variant policy).
- **Api:** `MediathekApiEndpoints` reads the option.
- **Behavior change:** searches now also return pre-released entries. `IncludeFuture=false` restores the previous behavior.
- **Not changed:** download failure classification (see design.md).
