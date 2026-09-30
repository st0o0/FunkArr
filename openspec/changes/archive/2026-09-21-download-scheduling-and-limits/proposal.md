## Why

FunkArr currently dispatches downloads immediately with no control over when or how fast they run. Users on residential connections need to restrict downloads to off-peak hours (e.g., 23:00–02:00) and optionally cap bandwidth so other services remain usable.

## What Changes

- Add optional time-slot scheduling to `DownloadManager` - new downloads are only dispatched during configured time windows, with timer-based wake at window start
- Add optional speed limit passed through to FFmpeg protocol-level rate limiting
- Extend `DownloadOptions` with `DownloadSchedule` (list of time slots) and `SpeedLimitBytesPerSecond`
- Expose schedule/limit settings via internal API for UI consumption
- Running downloads always finish regardless of window - no abort/requeue complexity

## Capabilities

### New Capabilities
- `download-scheduling`: Time-slot-based dispatch control in DownloadManager - checks server-local time against configured windows before dispatching, schedules timer for next window start when outside
- `download-speed-limit`: Optional bandwidth cap applied via FFmpeg input protocol options

### Modified Capabilities
- `download-options`: Add `DownloadSchedule` and `SpeedLimitBytesPerSecond` configuration properties
- `download-manager`: DispatchNext gains time-window gate and scheduler timer integration

## Impact

- `FunkArr.Core/DownloadOptions.cs` - new properties and nested `DownloadTimeSlot` model
- `FunkArr.Download/DownloadManager.cs` + `DownloadManagerState.cs` - scheduling gate in DispatchNext, timer management
- `FunkArr.Download/FfmpegRunner.cs` - speed limit args in FFmpeg command builder
- `FunkArr.Api` - new endpoint(s) for reading schedule/limit config
- `FunkArr.Messages` - query/response for schedule settings
- No persistence changes - schedule config is options-based, not persisted actor state
- No breaking changes to existing download flow - all new behavior is opt-in via config
