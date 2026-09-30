## 1. Dependencies

- [x] 1.1 Add FFMpegCore package to Directory.Packages.props
- [x] 1.2 Add FFMpegCore PackageReference to FunkArr.Download.csproj

## 2. Interface and types

- [x] 2.1 Create `IFfmpegRunner` interface and `FfmpegResult` record in FunkArr.Download (keep existing `ProgressUpdate` record)
- [x] 2.2 Implement `FfmpegRunner` class backed by FFMpegCore: video-only and video+subtitle argument building, progress parsing via NotifyOnOutput, CancellationToken bridging, stderr capture

## 3. Wire up

- [x] 3.1 Register `IFfmpegRunner` as singleton in ServiceSetupContainer
- [x] 3.2 Update DownloadWorker: inject `IFfmpegRunner`, replace `FfmpegRunner.Run(Self, ...)` with `_runner.RunAsync(...).PipeTo(Self)`, replace `ProcessExited` handling with `FfmpegResult` handling

## 4. Cleanup

- [x] 4.1 Delete FfmpegProcess.cs, FfmpegArgumentBuilder.cs, FfmpegProgressParser.cs
- [x] 4.2 Delete FfmpegArgumentBuilderTests.cs, FfmpegProgressParserTests.cs

## 5. Tests

- [x] 5.1 Add FfmpegRunnerTests verifying argument construction for video-only and video+subtitle cases
- [x] 5.2 Verify build passes and all existing tests still pass
