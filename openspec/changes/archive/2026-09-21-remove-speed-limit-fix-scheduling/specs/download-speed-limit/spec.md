## REMOVED Requirements

### Requirement: FfmpegRunner applies speed limit when configured
**Reason**: Used FFmpeg's `-maxrate`/`-bufsize` flags which control output encoder bitrate, not download speed. During codec-copy remux these flags are silently ignored. The feature was non-functional.
**Migration**: Remove `speedLimitBytesPerSecond` parameter from `IFfmpegRunner.RunAsync()`. No behavioral change since the parameter had no effect during remux.

### Requirement: DownloadWorker passes speed limit to FfmpegRunner
**Reason**: Removed together with the speed limit feature. The worker read `SpeedLimitBytesPerSecond` from options and passed it through, but the value was never applied.
**Migration**: Remove speed limit reading from `DownloadWorker.StartFfmpeg()`. No behavioral change.
