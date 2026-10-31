# Design

## Context

`QueryMediathek` carries a `Future` flag that maps to MediathekViewWeb's `future` parameter, which only filters `timestamp <= now + 1h` when `false` (`server/SearchEngine.ts`). The API has no other date filter. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:** pre-released episodes reach Sonarr/Radarr as early as they are downloadable, without degrading search results.

**Non-Goals:**
- Changing download failure classification for pre-released entries (see below).
- A configuration switch.

## Decisions

### Pre-released entries only in the feed

Sonarr's automatic searches only cover aired episodes, and by the time an episode has aired MediathekViewWeb returns it with `future=false` as well. In search results, upcoming entries can only take up room: sorted by date descending, the entries furthest in the future come first (live: Tele-Gym 48 of 50, die nordstory 25 of 50). The RSS sync is where Sonarr accepts releases for monitored episodes before their air date, so the feed is the one place where pre-released entries add value.

The feed sorts by `filmlisteTimestamp`; upcoming entries cannot displace anything there, so a single `future=true` query suffices.

Alternatives tried during the change and dropped:
- `future=true` for searches with two merged queries (`future=false` + `future=true`) per search: doubled the load on every search.
- `future=true` for searches with adaptive paging inside `MediathekViewWebManager`: one request for most shows, but extra state and complexity in the manager for a case Sonarr never searches.
- An `IncludeFuture` configuration option: no remaining purpose.

### Lead-time cap

The feed drops entries more than 90 days ahead. Live data had 3 such entries out of 1,141 future entries (up to +1159 days, data errors).

### No change to download failure classification

Issue #205 asked to treat a not-yet-available video URL as a retryable failure. This change does not do that: entries MVW returns with a future timestamp were verified manually from Germany to be downloadable, and the download worker cannot tell "not yet published" from "expired". If real-world reports show announced-but-unavailable entries, a follow-up can carry the air date in the NZB payload and retry 404 only for pre-released items.

### Sonarr/Radarr compatibility

Sonarr rejects releases before air date only when the air date restriction of a release profile is enabled (`AirDateSpecification`). The docs point users who want to wait for the broadcast to that setting. `AiredAt` stays the scheduled broadcast time, which is what TVDB lists, so date-based enrichment is unaffected.

## Risks / Trade-offs

- [Manual interactive search for an unaired episode finds nothing] → Rare; the episode arrives through the RSS sync.
- [Livestream or placeholder entries] → Filtered by `durationMin=300` in the feed query and by ruleset evaluation.
