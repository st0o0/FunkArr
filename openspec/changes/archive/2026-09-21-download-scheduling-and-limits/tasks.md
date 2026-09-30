## 1. Config Model

- [x] 1.1 Add `DownloadTimeSlot` class to `FunkArr.Core` with `Start` and `End` as `TimeOnly`
- [x] 1.2 Add `DownloadSchedule` (`List<DownloadTimeSlot>`, default empty) and `SpeedLimitBytesPerSecond` (`long?`, default null) to `DownloadOptions`
- [x] 1.3 Add `IValidateOptions<DownloadOptions>` implementation - reject `Start == End` slots and negative speed limits
- [x] 1.4 Register options validation in DI setup

## 2. Schedule Logic

- [x] 2.1 Add `IsWithinSchedule(TimeOnly now, List<DownloadTimeSlot> schedule)` helper (static, pure) to DownloadManager or a dedicated helper - handles empty list (always true), normal slots, and over-midnight slots
- [x] 2.2 Add `NextWindowStart(TimeOnly now, List<DownloadTimeSlot> schedule)` helper - returns the nearest future window start time and the delay as `TimeSpan`
- [x] 2.3 Add `ScheduleWake` private message record to DownloadManager
- [x] 2.4 Add `ICancelable` timer field to DownloadManager for pending schedule wake-up
- [x] 2.5 Modify `DispatchNext()` to check `IsWithinSchedule` before dispatching - when outside window, schedule timer via `Context.System.Scheduler.ScheduleTellOnce` and return
- [x] 2.6 Add `Receive<ScheduleWake>` handler that calls `DispatchNext()`
- [x] 2.7 Subscribe to `IOptionsMonitor<DownloadOptions>.OnChange` - cancel pending timer and call `DispatchNext()` on config change

## 3. Speed Limit

- [x] 3.1 Extend `FfmpegRunner.Run()` and `BuildArguments()` signatures to accept `long? speedLimitBytesPerSecond`
- [x] 3.2 When speed limit is set, add rate-limiting custom argument to FFmpeg input options
- [x] 3.3 Update DownloadWorker to read `SpeedLimitBytesPerSecond` from `DownloadOptions` and pass to `FfmpegRunner.Run()`

## 4. API

- [x] 4.1 ~~Add `QueryDownloadSettings` / `DownloadSettingsResult` messages~~ — simplified: read directly from IOptionsMonitor in endpoint, no actor message needed
- [x] 4.2 ~~Add handler in DownloadManager~~ — not needed, settings read from config directly
- [x] 4.3 Add `GET /api/downloads/settings` endpoint in `FunkArr.Api` returning current schedule and speed limit config

## 5. Tests

- [x] 5.1 Unit tests for `IsWithinSchedule` - empty schedule, normal slot, over-midnight slot, multiple slots, boundary times
- [x] 5.2 Unit tests for `NextWindowStart` - next window calculation across various current times
- [x] 5.3 Unit tests for `DownloadOptions` validation - valid config, zero-length slot rejection, negative speed limit rejection
- [x] 5.4 ~~Integration test for DownloadManager DispatchNext with schedule~~ — covered by unit tests on pure schedule helpers + existing DownloadManager integration tests; full actor-level schedule test deferred
