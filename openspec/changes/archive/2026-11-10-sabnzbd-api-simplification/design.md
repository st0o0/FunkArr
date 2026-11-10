# Design: SABnzbd API Simplification

## Architecture After

```
Sonarr/Radarr
    │
    ├── Newznab (/index/api)     ← unchanged
    │     t=caps, t=tvsearch, t=movie, t=search, t=get
    │
    └── SABnzbd (/download/api)  ← reduced to Sonarr/Radarr modes only
          version, get_config, fullstatus, addfile,
          queue(+delete), history(+delete), retry

FunkArr UI
    │
    └── Internal API (/api/downloads/*)  ← sole owner of UI operations
          pause, resume, priority, swap, move, force-start,
          history stats/detail/categories, settings, queue stream
```

## Priority Model After

```
Before (5 values):              After (4 values):
  Unknown = 0                     Unknown = 0
  Paused  = 1  ← REMOVE          Low     = 1  ← renumber
  Low     = 2                     Normal  = 2
  Normal  = 3                     High    = 3
  High    = 4
```

Wait — renumbering would break persistence. Keep the gap instead:

```
After (4 values, stable numbering):
  Unknown = 0
  Low     = 2       ← was 2, stays 2
  Normal  = 3       ← was 3, stays 3
  High    = 4       ← was 4, stays 4
  (Paused = 1 removed — recovery maps it to Low)
```

This applies to all three enums: `DownloadPriority`, `QueuePriority`,
`PersistedDownloadPriority`.

## SABnzbd Controller Changes

`SabnzbdController` dispatches by `mode` query param. Remove cases:

```csharp
// REMOVE these from the GET handler switch:
"pause"  => ...
"resume" => ...

// REMOVE from queue sub-dispatch (name param):
"priority" => ...
"switch"   => ...
```

Return `SabnzbdError("Invalid mode")` for removed modes — same as any unknown mode.

## SabnzbdResponseMapper Changes

Remove:
- `MapPriority(DownloadPriority)` → `SabnzbdPriority` (no longer needed for queue slots)
- `MapSabnzbdPriority(string?)` → `DownloadPriority` (no priority inbound from SABnzbd)

The queue response `QueueSlot.Priority` field: keep it in the wire model for compatibility
but always return `"Normal"` (SABnzbd clients don't use it).

## SabnzbdDownloadService Changes

Remove:
- `SetPriority()` method (lines 151-185)
- `Pause()` / `Resume()` methods
- `SwapDownloads()` method

Keep:
- `AddFile()` — NZB upload
- `DeleteFromQueue()` — queue item removal
- `RetryDownload()` — retry failed
- `DeleteHistory()` — history cleanup

## DownloadManager State Changes

`DispatchNext()` currently skips items with `Priority == Paused` (line 389):
```csharp
var next = _state.Queued.FirstOrDefault(e =>
    !dispatching.Contains(e.Id) && e.Priority != DownloadPriority.Paused);
```

Remove the `Paused` check. Queue-level pause (`_state.Paused`) already prevents
dispatch — per-item priority-pause is redundant.

## Persistence Migration

On snapshot recovery, if a `PersistedDownloadPriority` value of 1 (old `Paused`) is
encountered, map it to `Low` (2). This is a one-time forward migration — no backward
compatibility needed (v0.x).

```csharp
// In PersistenceMapping.ToDomain():
PersistedDownloadPriority p => p switch
{
    (PersistedDownloadPriority)1 => DownloadPriority.Low,  // migrate Paused→Low
    PersistedDownloadPriority.Low => DownloadPriority.Low,
    ...
}
```

## QueuePriority Validation (Internal API)

The priority endpoint validates:
```csharp
if (body.Priority is QueuePriority.Unknown or <= 0)
    return Results.Problem("Priority must be Low (2), Normal (3), or High (4)", statusCode: 400);
```

## Frontend Error Handling

`throwIfNotOk()` in `rulesets.ts`:

```typescript
async function throwIfNotOk(res: Response): Promise<void> {
  if (res.ok) return
  if (res.status === 422) {
    const body = await res.json()
    if (body.errors) throw new ValidationFailedError(body.errors)
  }
  // NEW: extract ProblemDetails detail for 4xx
  const contentType = res.headers.get('content-type') ?? ''
  if (res.status >= 400 && res.status < 500 && contentType.includes('json')) {
    try {
      const body = await res.json()
      if (body.detail) throw new Error(body.detail)
    } catch (e) { if (e instanceof Error && e.message !== body.detail) { /* ignore parse errors */ } }
  }
  throw new Error(`${res.status} ${res.statusText}`)
}
```

## Test Impact

### Remove tests
- `SabnzbdDownloadServiceTests`: pause, resume, priority, swap test cases
- `SabnzbdQueueServiceTests`: priority mapping tests
- `SabnzbdResponseMapperTests`: `MapPriority` tests
- `SabnzbdWireTests`: pause/resume/priority/swap wire format tests

### Update tests
- `EnumMappingExtensionsTests`: remove Paused arm from QueuePriority mapping tests
- `DownloadApiEndpointTests`: add validation test for Unknown/Paused priority rejection
- `DownloadManagerTests` (if they exist): remove Paused-priority dispatch skip tests

### Add tests
- Priority endpoint: 400 for Unknown (0) and removed Paused (1)
- Frontend: error toast shows ProblemDetails detail text

## E2E Test Plan Updates

- Remove SABnzbd pause/resume tests (21.10, 21.11) — these modes no longer exist
- Remove SABnzbd priority test (21.8) — mode removed
- Remove SABnzbd swap test (21.9) — mode removed
- Keep internal API pause/resume/priority/swap tests (28.4-28.9) — these are the sole path now
- Update priority enum values in test plan: Low=2, Normal=3, High=4 (no Paused)
