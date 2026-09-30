## Why

Persistent actors mix state mutation and side-effects (Tell, Sender.Tell, DispatchNext,
external process launches) inside Persist callbacks. This makes it hard to distinguish
what changes state vs. what notifies other actors. Two unbounded-growth singletons
(DownloadManager, DownloadHistoryManager) lack SaveSnapshot, meaning recovery replays
the entire journal. Additionally, one ContinueWith usage bypasses Akka threading
guarantees, and two DateTime.UtcNow calls bypass TimeProvider.

## What Changes

- Extract non-trivial side-effects from Persist callbacks into DeferAsync in 10 handlers
  across DownloadManager (8), DownloadWorker (2+2 result handlers)
- Keep simple side-effects (single Sender.Tell, logging, metrics) inline in 9 handlers
- Add interval-based SaveSnapshot to DownloadManager and DownloadHistoryManager
- Replace ContinueWith with PipeTo-based pattern in DownloadManager fan-out
- Inject TimeProvider into TvdbClient and NzbService, replacing DateTime.UtcNow

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `akka-persistence`: Adding DeferAsync convention and differentiated snapshot rules
- `download-manager`: Refactoring Persist callbacks, adding SaveSnapshot
- `download-worker`: Refactoring Persist callbacks for non-trivial side-effects
- `download-history`: Adding SaveSnapshot to DownloadHistoryManager

## Impact

- `src/FunkArr.Download/DownloadManager.cs` - 10 Persist handlers refactored, SaveSnapshot added, ContinueWith replaced
- `src/FunkArr.Download/DownloadWorker.cs` - 4 Persist handlers refactored
- `src/FunkArr.Download/DownloadHistoryManager.cs` - SaveSnapshot added
- `src/FunkArr.Enrichment/TvdbClient.cs` - TimeProvider injected
- `src/FunkArr.ArrApi/Newznab/NzbService.cs` - TimeProvider injected
- Test projects for Download, Enrichment, ArrApi may need TimeProvider in DI setup
- No API changes, no message changes, no persistence schema changes
- Behavioral: side-effects now execute after Persist callback completes (DeferAsync ordering)
