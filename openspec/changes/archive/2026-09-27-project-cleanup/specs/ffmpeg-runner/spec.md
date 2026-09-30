## REMOVED Requirements

### Requirement: FfmpegRunner is a static facade
**Reason:** FfmpegRunner functionality absorbed into Remuxer. The class and its IFfmpegRunner interface are deleted.
**Migration:** BuildArguments, ParseProgressLine, ClassifyFailure, ExtractError move to internal static methods in the Remuxer or a companion helper class. RunAsync logic moves into Remuxer.RunAsync.

### Requirement: FfmpegRunner interface includes language
**Reason:** IFfmpegRunner interface removed. Language is passed via RemuxOptions record internally.
**Migration:** Remuxer constructs RemuxOptions with SubtitleLanguage from the subtitle track.

### Requirement: FfmpegRunner sends ProgressUpdate messages
**Reason:** IFfmpegRunner interface removed. Progress reporting moves into Remuxer.
**Migration:** Remuxer invokes the onProgress callback directly.

### Requirement: FfmpegRunner sends ProcessExited on completion
**Reason:** IFfmpegRunner interface removed. Result handling moves into Remuxer.
**Migration:** Remuxer returns FfmpegResult directly.

### Requirement: FfmpegRunner supports cancellation
**Reason:** IFfmpegRunner interface removed. Cancellation handling moves into Remuxer.
**Migration:** Remuxer passes CancellationToken to FFMpegCore's CancellableThrough.

### Requirement: FfmpegRunner injects proxy as input-level argument
**Reason:** IFfmpegRunner interface removed. Proxy injection moves into the BuildArguments method inside Remuxer.
**Migration:** BuildArguments reads ProxyUrl from RemuxOptions.
