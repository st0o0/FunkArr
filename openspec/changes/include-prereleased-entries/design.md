# Design

## Context

`TvSearchWorkerState` and `MovieSearchWorkerState` each build two `QueryMediathek` messages (free query, resolved topic) with `Future: false`; `MediathekApiEndpoints` builds one more. The variant policy (#203) already introduced the pattern of passing an option snapshot from the worker into its state. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:** every query FunkArr builds for searches includes pre-released entries.

**Non-Goals:**
- Changing download failure classification for pre-released entries (see below).
- A configuration switch, per-ruleset or per-request overrides.

## Decisions

### Fixed behaviour, no option

An `IncludeFuture` option was implemented first and then removed. Pre-released entries were verified to be downloadable, adaptive paging removes the only downside (crowding the result window), and users who want to wait for the broadcast already have Sonarr's air date restriction. An option would only add a code path nobody needs. Version 0.x allows dropping it without migration.

*Alternative:* carry the flag on `SearchSeries` / `SearchMovie`. Rejected: not a request property, and ArrApi would have to know about it.

### No change to download failure classification

Issue #205 asked to treat a not-yet-available video URL as a retryable failure. This change does not do that:

- The download worker only knows the NZB payload (URL, title, channel, duration, size). It cannot tell "not yet published" from "expired", so a 404 would have to become retryable for every download. The `download-retry` spec deliberately classifies 404 as permanent.
- Retries are disabled by default (`RetryEnabled=false`), so the change would not help most users.
- Entries MVW returns with a future timestamp are, in practice, already published ("vorab in der Mediathek").

If real-world reports show announced-but-unavailable entries, a follow-up can carry the air date in the NZB payload and retry 404 only for pre-released items.

### Sonarr/Radarr compatibility

Sonarr rejects releases before air date only when the user enabled the air date restriction in a release profile (`AirDateSpecification`); otherwise it accepts them. The docs point users who want to wait for the broadcast to that setting. `AiredAt` stays the scheduled broadcast time, which is what TVDB lists, so date-based enrichment is unaffected.

### Adaptive paging in the Mediathek manager

Live data (October 2026, 717k entries): 1,141 entries lie in the future, added a median 25 days before broadcast. For 20 typical shows, 15 have 0-6 upcoming entries; only a few daily formats flood the window (Tele-Gym 48 of 50, die nordstory 25 of 50). The MediathekViewWeb API has no date-range filter (only `future`, durations and text queries; see `server/SearchEngine.ts`), so "upcoming only" cannot be queried directly.

Options considered:
- Two parallel queries (`future=false` + `future=true`, merged): deterministic, but always doubles the load and, sorted by date descending, the `future=true` window holds the farthest entries and cuts the nearest ones for flooded shows.
- One query with a larger size: cheap, but not guaranteed (largest observed need was 75 for a window of 50).
- **Chosen:** one `future=true` query; if the page holds fewer aired entries than requested and the page was full, fetch the next page, at most 3. One request for most shows, exact aired window, and the nearest upcoming entries are kept.

The logic lives in `MediathekQueryPlan`, owned by `MediathekViewWebManager`, which already tracks pending requests by id. Callers send one `QueryMediathek` and receive one response; queue-full on any page is reported once, so the search workers' retry logic is unchanged. Paging applies only to `timestamp desc`; the feed sorts by `filmlisteTimestamp`, where upcoming entries cannot displace anything.

### Lead-time cap

Entries more than 90 days ahead are dropped. Live data had 3 such entries out of 1,141 (up to +1159 days, data errors).

## Risks / Trade-offs

- [Future entries crowd out older ones in `sortBy=timestamp desc` queries with a size limit] → Live data showed up to 48 of 50 slots taken by future entries for daily shows; solved by adaptive paging in the Mediathek manager.
- [Livestream or placeholder entries] → Already filtered by `durationMin=300` in TV/movie queries.
