# SABnzbd API Simplification

## Problem

The SABnzbd-compatible download API (`/download/api`) implements 5 modes that no Arr
client ever calls — they exist solely for the FunkArr UI, which already has its own
internal REST endpoints for the same operations. This creates:

- **10 fully duplicated operations** between SABnzbd and internal API, each sending the
  same actor message through separate mapping chains
- **3 priority representations** for one concept: SABnzbd wire int (-2..2),
  `SabnzbdPriority` string enum, `QueuePriority` int enum — all flowing through
  `DownloadPriority`
- **`DownloadPriority.Paused`** existing in the domain model solely because SABnzbd
  overloads priority=-2 as a pause mechanism — leaking into the internal API where
  `{"priority":1}` silently pauses a download
- **ForceStart hidden inside priority** — SABnzbd priority=2 triggers ForceStart via
  special-case code, a protocol quirk that shouldn't ripple into domain

Evidence from Sonarr/Radarr source code: they call exactly 6 SABnzbd modes (version,
get_config, fullstatus, addfile, queue+delete, history+delete+retry). They never call
pause, resume, priority, or switch.

## Solution

Remove UI-only operations from the SABnzbd API. The internal REST API already covers
them.

### Remove from SABnzbd API

| Mode | Replacement (internal API) |
|------|--------------------------|
| `mode=pause` | `POST /api/downloads/queue/pause` |
| `mode=resume` | `POST /api/downloads/queue/resume` |
| `mode=queue&name=priority` | `POST /api/downloads/queue/{id}/priority` |
| `mode=queue&name=switch` | `POST /api/downloads/queue/swap` |

### Keep in SABnzbd API (Sonarr/Radarr use these)

- `mode=version` — connectivity test
- `mode=get_config` — category config
- `mode=fullstatus` — status read
- `mode=addfile` — NZB upload (POST multipart)
- `mode=queue` — queue listing
- `mode=queue&name=delete` — remove from queue
- `mode=history` — history listing
- `mode=history&name=delete` — remove from history
- `mode=retry` — retry failed download

### Domain cleanup

- Remove `DownloadPriority.Paused` from `FunkArr.Messages` — pause is a status, not a
  priority level. The queue-level pause (`PauseDownloads`/`ResumeDownloads`) and
  force-start already cover all pause semantics.
- Remove `PersistedDownloadPriority.Paused` — requires snapshot migration (set Paused→Low
  on recovery)
- Remove `SabnzbdPriority` enum entirely — no longer needed without outbound priority
  mapping
- Remove `MapSabnzbdPriority()` and `MapPriority()` from `SabnzbdResponseMapper`
- Remove `QueuePriority.Paused` from the internal API enum
- Simplify `DispatchNext()` — remove the `Priority != Paused` skip check; items are
  paused via queue-level pause, not per-item priority

### Internal API fix (bundled)

- Block `QueuePriority.Unknown` (0) on the priority endpoint — return 400
- Validate priority is Low/Normal/High only

### Frontend fix (bundled)

- `throwIfNotOk()` in `rulesets.ts`: extract ProblemDetails `detail` field for all 4xx
  responses, not just 422

## Scope

- **In scope**: SABnzbd mode removal, domain priority cleanup, persistence migration,
  internal API validation, frontend error handling
- **Out of scope**: Newznab API changes, feed pipeline changes, internal API restructuring

## Risk

- **Low**: Sonarr/Radarr never call the removed modes (verified from source code)
- **Persistence migration**: Existing `Paused` priority values in snapshots need migration
  to `Low` on recovery. Bounded impact — only affects items that were explicitly paused
  via the SABnzbd priority mechanism (unlikely in practice since the UI uses the internal
  API's pause endpoint)
