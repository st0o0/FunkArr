# FFmpeg Runner

## Purpose

~~Removed.~~ FfmpegRunner functionality has been absorbed into Remuxer. The class and its IFfmpegRunner interface are deleted.

BuildArguments, ParseProgressLine, ClassifyFailure, ExtractError move to internal static methods in the Remuxer or a companion helper class. RunAsync logic moves into Remuxer.RunAsync. Language is passed via RemuxOptions record internally. Progress reporting, result handling, and cancellation are handled by Remuxer directly.

## Requirements

_All requirements removed. See remuxer and ffmpeg-process specs for the replacement._

