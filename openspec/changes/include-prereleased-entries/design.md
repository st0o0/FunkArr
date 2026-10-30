# Design

## Context

`TvSearchWorkerState` and `MovieSearchWorkerState` each build two `QueryMediathek` messages (free query, resolved topic) with `Future: false`; `MediathekApiEndpoints` builds one more. The variant policy (#203) already introduced the pattern of passing an option snapshot from the worker into its state. See proposal.md for motivation.

## Goals / Non-Goals

**Goals:** one option, honored by every query FunkArr builds for searches.

**Non-Goals:**
- Changing download failure classification for pre-released entries (see below).
- Per-ruleset or per-request overrides.

## Decisions

### `SearchOptions` in Core, passed like the variant policy

`SearchOptions` (`FunkArr:Search`, `IncludeFuture = true`) is bound in `SearchSetupContainer`. The workers receive `IOptionsMonitor<SearchOptions>?` as an optional constructor parameter and pass `CurrentValue.IncludeFuture` to `From(...)` of their state; the default is `true` when no options are given (tests). `MediathekApiEndpoints` injects `IOptionsMonitor<SearchOptions>` into the handler.

*Alternative:* carry the flag on `SearchSeries` / `SearchMovie`. Rejected: it is a deployment setting, not a request property, and ArrApi would have to know about it.

### No change to download failure classification

Issue #205 asked to treat a not-yet-available video URL as a retryable failure. This change does not do that:

- The download worker only knows the NZB payload (URL, title, channel, duration, size). It cannot tell "not yet published" from "expired", so a 404 would have to become retryable for every download. The `download-retry` spec deliberately classifies 404 as permanent.
- Retries are disabled by default (`RetryEnabled=false`), so the change would not help most users.
- Entries MVW returns with a future timestamp are, in practice, already published ("vorab in der Mediathek").

If real-world reports show announced-but-unavailable entries, a follow-up can carry the air date in the NZB payload and retry 404 only for pre-released items.

### Sonarr/Radarr compatibility

Sonarr rejects releases before air date only when the user enabled the air date restriction in a release profile (`AirDateSpecification`); otherwise it accepts them. The docs point users who want to wait for the broadcast to that setting. `AiredAt` stays the scheduled broadcast time, which is what TVDB lists, so date-based enrichment is unaffected.

## Risks / Trade-offs

- [Future entries crowd out older ones in `sortBy=timestamp desc` queries with a size limit] → Topic-scoped queries contain few future entries; the limit (50) is unchanged. Users can disable the option.
- [Livestream or placeholder entries] → Already filtered by `durationMin=300` in TV/movie queries.
