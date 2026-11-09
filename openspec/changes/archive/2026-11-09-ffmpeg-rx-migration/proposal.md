## Why

FunkArr.Download currently uses FFMpegCore 5.5.0 to drive FFmpeg process execution. The project owner has published FFmpeg.Rx — a typed, reactive FFmpeg wrapper built on CliWrap + R3 — which provides the same capabilities with a stronger type system (typed codecs, stream builders, metadata/disposition on streams) and built-in error classification and progress parsing that FunkArr currently duplicates by hand. Migrating eliminates ~130 lines of redundant code (progress parsing, error extraction, failure classification) and replaces string-based WithCustomArgument calls with typed builder API where possible.

## What Changes

- Replace the `FFMpegCore` NuGet dependency with `FFmpeg.Rx` in FunkArr.Download
- Rewrite `FfmpegProcess.cs` to use `Ffmpeg.Input(uri).WithProgressReporting().Output(...)` builder API and `IFfmpegSession.RunAsync(ct)` instead of FFMpegCore's `FFMpegArguments` fluent chain
- Remove hand-rolled `ParseProgressLine`, `ExtractError`, and `ClassifyFailure` methods (now provided by FFmpeg.Rx's `ProgressParser` and `ErrorClassifier`)
- Map FFmpeg.Rx's `ProgressEvent` (BytesWritten, TimeUs, Speed) to FunkArr's `ProgressUpdate` (TotalSize, OutTimeUs, Speed)
- Map FFmpeg.Rx's `FailureKind` (Unknown=0, Transient=1, Permanent=2) to FunkArr.Messages `FailureKind` (Transient=0, Permanent=1) via exhaustive switch
- Map FFmpeg.Rx's `FfmpegResult.Duration` (TimeSpan) to FunkArr's `FfmpegResult.ElapsedSeconds` (int)
- Replace string-based subtitle args (`-c:s srt`, `-disposition:s:0 0`, `-metadata:s:s:0 language=...`) with typed `StreamBuilder.Encode(SubtitleCodec.Srt).Disposition("0").Metadata("language", lang)`
- Replace string-based `-progress pipe:1` with `.WithProgressReporting()`
- Update tests: remove ParseProgressLine/ExtractError/ClassifyFailure tests (now in FFmpeg.Rx.Tests), rewrite BuildArguments tests for new builder output

## Capabilities

### New Capabilities

_None — this is a like-for-like dependency swap._

### Modified Capabilities

- `ffmpeg-process`: Requirements change from FFMpegCore-specific API (FFMpegArguments, CopyChannel, SelectStream, ProcessAsynchronously, FFMpegException) to FFmpeg.Rx API (PipelineBuilder, StreamBuilder, IFfmpegSession, FfmpegResult). Behavioral requirements (stream mapping, BSF tolerance, subtitle handling, progress reporting, cancellation) remain identical.

## Impact

- **Dependencies**: `FFMpegCore` removed, `FFmpeg.Rx` added (brings transitive `CliWrap` + `R3`)
- **Code**: `FfmpegProcess.cs` rewritten (~60 lines vs ~190), test file updated
- **Unchanged**: `Remuxer.cs`, `IRemuxer`, `RemuxOptions`, `FfmpegInput`, `DownloadWorker`, `DownloadWorkerState` — all operate above the `IFfmpegProcess` boundary
- **Risk**: Minimal — `IFfmpegProcess` interface isolates the change, behavioral contract stays the same
