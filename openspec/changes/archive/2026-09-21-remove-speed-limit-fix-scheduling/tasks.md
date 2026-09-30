## 1. Remove speed limit from core interfaces

- [x] 1.1 Remove `SpeedLimitBytesPerSecond` property from `DownloadOptions` in `FunkArr.Core/DownloadOptions.cs`
- [x] 1.2 Remove speed limit validation from `DownloadOptionsValidator` in `FunkArr.Core/DownloadOptionsValidator.cs`
- [x] 1.3 Remove `speedLimitBytesPerSecond` parameter from `IFfmpegRunner.RunAsync()` in `FunkArr.Download/IFfmpegRunner.cs`
- [x] 1.4 Remove `speedLimitBytesPerSecond` parameter from `IRemuxer.RunAsync()` in `FunkArr.Download/IRemuxer.cs`

## 2. Remove speed limit from implementations

- [x] 2.1 Remove `speedLimitBytesPerSecond` parameter and `-maxrate`/`-bufsize` logic from `FfmpegRunner` in `FunkArr.Download/FfmpegRunner.cs`
- [x] 2.2 Remove `speedLimitBytesPerSecond` passthrough from `Remuxer` in `FunkArr.Download/Remuxer.cs`
- [x] 2.3 Remove speed limit reading from `DownloadWorker.StartFfmpeg()` in `FunkArr.Download/DownloadWorker.cs`
- [x] 2.4 Remove `SpeedLimitBytesPerSecond` from `DownloadSettingsResponse` and settings endpoint in `FunkArr.Api/Models/DownloadSettings.cs` and `FunkArr.Api/DownloadsApiEndpoints.cs`

## 3. Remove speed limit tests

- [x] 3.1 Remove `BuildArguments_with_speed_limit_includes_maxrate` and `BuildArguments_without_speed_limit_excludes_maxrate` tests from `FunkArr.Download.Tests/FfmpegRunnerTests.cs`
- [x] 3.2 Remove speed limit validation tests from `FunkArr.Download.Tests/DownloadOptionsValidatorTests.cs`

## 4. Inject TimeProvider into DownloadManager

- [x] 4.1 Add `TimeProvider` constructor parameter to `DownloadManager` in `FunkArr.Download/DownloadManager.cs`
- [x] 4.2 Replace `DateTime.Now` with `_timeProvider.GetLocalNow()` in `DispatchNext()` method

## 5. Fix spec Purpose sections

- [x] 5.1 Update Purpose in `openspec/specs/download-scheduling/spec.md` (replace TBD with actual purpose)
- [x] 5.2 Delete `openspec/specs/download-speed-limit/spec.md` entirely
- [x] 5.3 Update `openspec/specs/download-options/spec.md` to remove speed limit requirements

## 6. Verify

- [x] 6.1 Run `dotnet build src/FunkArr.slnx` — must compile cleanly
- [x] 6.2 Run `dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj` — all tests pass
- [x] 6.3 Run `dotnet format src/FunkArr.slnx --verify-no-changes` — formatting clean
