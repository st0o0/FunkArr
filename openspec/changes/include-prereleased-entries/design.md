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

An `IncludeFuture` option was implemented first and then removed. Pre-released entries were verified to be downloadable, the two-query merge removes the only downside (crowding the result window), and users who want to wait for the broadcast already have Sonarr's air date restriction. An option would only add a code path nobody needs. Version 0.x allows dropping it without migration.

*Alternative:* carry the flag on `SearchSeries` / `SearchMovie`. Rejected: not a request property, and ArrApi would have to know about it.

### No change to download failure classification

Issue #205 asked to treat a not-yet-available video URL as a retryable failure. This change does not do that:

- The download worker only knows the NZB payload (URL, title, channel, duration, size). It cannot tell "not yet published" from "expired", so a 404 would have to become retryable for every download. The `download-retry` spec deliberately classifies 404 as permanent.
- Retries are disabled by default (`RetryEnabled=false`), so the change would not help most users.
- Entries MVW returns with a future timestamp are, in practice, already published ("vorab in der Mediathek").

If real-world reports show announced-but-unavailable entries, a follow-up can carry the air date in the NZB payload and retry 404 only for pre-released items.

### Sonarr/Radarr compatibility

Sonarr rejects releases before air date only when the user enabled the air date restriction in a release profile (`AirDateSpecification`); otherwise it accepts them. The docs point users who want to wait for the broadcast to that setting. `AiredAt` stays the scheduled broadcast time, which is what TVDB lists, so date-based enrichment is unaffected.

## Risks / Trade-offs

- [Future entries crowd out older ones in `sortBy=timestamp desc` queries with a size limit] → Live data showed up to 48 of 50 slots taken by future entries for daily shows; solved by the separate `future=false` query (see `evaluation-workers`).
- [Livestream or placeholder entries] → Already filtered by `durationMin=300` in TV/movie queries.
