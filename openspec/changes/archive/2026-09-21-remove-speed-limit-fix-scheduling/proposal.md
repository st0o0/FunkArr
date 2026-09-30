## Why

The download speed limiting feature (`SpeedLimitBytesPerSecond`) uses FFmpeg's `-maxrate`/`-bufsize` flags, which control **output encoder bitrate** — not download speed. During a codec-copy remux (which is what FunkArr always does), these flags are silently ignored. The feature does nothing. Rather than fix it with a complex `-readrate` + ffprobe approach that adds little value for Mediathek streams, remove it entirely.

Additionally, the download scheduling feature uses `DateTime.Now` directly, making schedule logic untestable. The project already uses `TimeProvider` in `SearchResultCache` — the DownloadManager should follow the same pattern. Both the `download-scheduling` and `download-speed-limit` specs have placeholder Purpose sections ("TBD") that need fixing.

## What Changes

- **Remove** `SpeedLimitBytesPerSecond` from `DownloadOptions`, `DownloadOptionsValidator`, `FfmpegRunner`, `IFfmpegRunner`, `IRemuxer`, `Remuxer`, `DownloadWorker`, API settings response, and all related tests
- **Remove** the `download-speed-limit` spec entirely
- **Inject** `TimeProvider` into `DownloadManager` via constructor DI, replacing `DateTime.Now` in `DispatchNext()`
- **Fix** the Purpose section in `download-scheduling` spec
- **Update** the `download-options` spec to remove speed limit requirements

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `download-options`: Remove `SpeedLimitBytesPerSecond` property and its validation rule
- `download-speed-limit`: Delete entirely — capability removed
- `download-scheduling`: Fix Purpose section; add requirement that DownloadManager uses `TimeProvider` instead of `DateTime.Now`
- `download-manager`: Update to reflect TimeProvider injection and removal of speed limit passthrough
- `ffmpeg-process`: Remove speed limit parameter from runner interface and implementation

## Impact

- **FunkArr.Core**: `DownloadOptions` loses one property, `DownloadOptionsValidator` loses one check
- **FunkArr.Download**: `FfmpegRunner`, `IFfmpegRunner`, `IRemuxer`, `Remuxer`, `DownloadWorker` all lose speed limit parameters; `DownloadManager` gains `TimeProvider` constructor param
- **FunkArr.Api**: `DownloadSettingsResponse` loses `SpeedLimitBytesPerSecond`; `/api/downloads/settings` endpoint updated
- **FunkArr.Download.Tests**: Speed limit build-argument tests removed
- **OpenSpec specs**: 4 specs modified, 1 deleted
- **No persistence changes** — speed limit was never persisted
- **No breaking API change for arr clients** — speed limit was only on the internal settings endpoint
