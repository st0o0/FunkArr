## ADDED Requirements

### Requirement: FfmpegProcess uses FFmpeg.Rx fluent API

`FfmpegProcess.BuildSession` uses the FFmpeg.Rx 0.1.1 fluent API
(`Ffmpeg.Input().WithProgressReporting().Output()`) with typed input options
(UserAgent, HttpProxy) and stream-level BitstreamFilter instead of CustomArgs
strings. BuildPipeline, BuildInputs, BuildOutput methods are removed.
