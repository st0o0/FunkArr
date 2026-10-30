# Proposal

## Why

All MediathekViewWeb queries are sent with `future=false`, which hides every entry whose broadcast time lies in the future. Broadcasters, ZDF in particular, regularly publish episodes in the Mediathek **before** the TV broadcast. These episodes can be downloaded but stay invisible to FunkArr until air time, so users get them hours or days later than possible (#205). MVW's own RSS feed and MediathekArr both query with `future=true`.

## What Changes

- Pre-released entries are always included; there is no option to turn this off. Verified manually from Germany that such entries are downloadable.
- `QueryMediathek` loses its `Future` flag; the `MediathekViewWebManager` always queries with `future=true`.
- For date-sorted queries the manager pages adaptively so upcoming entries never push aired ones out of the requested window (one request for most shows, at most 3 pages), and drops entries more than 90 days ahead as data errors. Callers send one message and get one response.
- Documentation explains the behaviour and how to wait for the broadcast instead (Sonarr's release-profile air date restriction).

## Capabilities

### New Capabilities

- `prereleased-entries`: Mediathek entries published before their broadcast time are included in searches.

### Modified Capabilities

_None._ The existing query requirements in `tv-search` and `movie-search` do not state a `future` value; the new capability owns it.

## Impact

- **Messages:** `QueryMediathek` without `Future`.
- **Search:** `MediathekViewWebManager` + `MediathekQueryPlan` (paging, lead-time cap); `MediathekQueryBuilder` always sends `future=true`; workers send a single query.
- **Behavior change:** searches now also return pre-released entries, with no way back via configuration (version 0.x).
- **Not changed:** download failure classification (see design.md).
