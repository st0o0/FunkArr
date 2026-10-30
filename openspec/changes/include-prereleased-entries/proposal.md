# Proposal

## Why

All MediathekViewWeb queries are sent with `future=false`, which hides every entry whose broadcast time lies in the future. Broadcasters, ZDF in particular, regularly publish episodes in the Mediathek **before** the TV broadcast. These episodes can be downloaded but stay invisible to FunkArr until air time, so users get them hours or days later than possible (#205). MVW's own RSS feed and MediathekArr both query with `future=true`.

## What Changes

- **BREAKING (behaviour):** pre-released entries are always included; there is no option to turn this off. Verified manually from Germany that such entries are downloadable.
- TV and movie searches send their regular `future=false` query plus an identical `future=true` query and merge the results (see `evaluation-workers`); the internal Mediathek search (`/api/mediathek/search`) and the feed ingest query with `future=true`.
- Documentation explains the behaviour and how to wait for the broadcast instead (Sonarr's release-profile air date restriction).

## Capabilities

### New Capabilities

- `prereleased-entries`: Mediathek entries published before their broadcast time are included in searches.

### Modified Capabilities

_None._ The existing query requirements in `tv-search` and `movie-search` do not state a `future` value; the new capability owns it.

## Impact

- **Search:** worker states always build the `future=false` / `future=true` query pair.
- **Api:** `MediathekApiEndpoints` queries with `future=true`.
- **Behavior change:** searches now also return pre-released entries, with no way back via configuration (version 0.x).
- **Not changed:** download failure classification (see design.md).
