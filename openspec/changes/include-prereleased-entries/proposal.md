# Proposal

## Why

Broadcasters, ARD third programmes in particular, publish episodes in the Mediathek **before** the TV broadcast, a median of 25 days ahead (live data, October 2026). These episodes are downloadable but FunkArr never offered them (#205).

Sonarr only searches for episodes whose air date has passed (`EpisodeSearchService`, `SeriesSearchService`, `ReleaseSearchService` filter on `AirDateUtc < now`). Pre-released entries are therefore useless in search results; Sonarr can only pick them up through the RSS sync, where it accepts any monitored episode unless the air date restriction of a release profile is enabled.

## What Changes

- The feed ingest queries MediathekViewWeb with `future=true`, so pre-released episodes reach Sonarr/Radarr through the RSS feed as soon as they appear in the Filmliste.
- Searches (TV, movie, internal Mediathek search) keep `future=false`. This avoids upcoming entries crowding aired episodes out of the date-sorted result window (seen live: 48 of 50 slots for a daily show).
- The feed ingest drops entries more than 90 days ahead as data errors.
- Documentation explains the behaviour and how to wait for the broadcast (Sonarr's release-profile air date restriction).

## Capabilities

### New Capabilities

- `prereleased-entries`: pre-released Mediathek entries are offered through the RSS feed; searches stay limited to aired entries.

### Modified Capabilities

_None._

## Impact

- **Feed:** `FeedCrawler` queries with `future=true`; `FeedIngestPlanner.WithinLead` drops implausible dates.
- **Search / Api:** queries keep `future=false` (unchanged behaviour on `main`).
- No configuration option, no persistence change.
