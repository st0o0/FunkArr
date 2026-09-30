## Why

Subtitles are silently dropped for all downloads. Two bugs in the subtitle pipeline prevent any EBU-TT-D subtitle from being muxed into MKV files:

1. `SubtitlePreparer` fails to detect XML when it starts with an XML comment (`<!-- Profile: EBU-TT-D-Basic-DE -->`) instead of `<?xml>` or `<tt`. This affects ARD content and potentially other broadcasters. The subtitle URL is fetched but the response is not recognized as XML, so conversion never runs.

2. `TtmlToSrtConverter` does not handle the EBU-TT-D `documentStartOfProgramme` offset convention. Some ARD content uses timestamps starting at `10:00:00.000` (a documented EBU-TT-D feature). The converter produces SRT with 10-hour timestamps that are beyond the video duration, making subtitles invisible.

Additionally, telemetry cleanup from the metrics expansion: ObservableGauge fields are assigned but never read, Enrichment Meter naming is inconsistent, and `Search.Failed` is missing its `source` tag.

## What Changes

- **SubtitlePreparer**: Replace `StartsWith`-based XML detection with `XDocument.Parse` attempt followed by root element check for `tt` in any namespace. Fallback to WEBVTT/SRT detection for non-XML content. Add warning log when conversion produces empty output.
- **TtmlToSrtConverter**: Read `ebuttm:documentStartOfProgramme` metadata for authoritative offset. Fall back to auto-detecting offset from `min(begin)` when > 30 minutes. Subtract offset from all timestamps, clamp to >= 0.
- **Telemetry cleanup**: Remove unused ObservableGauge field assignments (use discard), rename Enrichment `_meter` to `Meter` (internal), add `source` tag to `Search.Failed`.
- **Tests**: Add tests for offset normalization, XML comment preamble detection, and `documentStartOfProgramme` parsing.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `remuxer`: Subtitle preparation now handles EBU-TT-D XML detection robustly and normalizes timestamp offsets per the EBU-TT-D specification.

## Impact

- `FunkArr.Download/SubtitlePreparer.cs` - format detection rewrite
- `FunkArr.Download/TtmlToSrtConverter.cs` - offset normalization
- `FunkArr.Download/Telemetry.cs` - ObservableGauge cleanup
- `FunkArr.Enrichment/Telemetry.cs` - Meter naming consistency
- `FunkArr.RuleSet/Telemetry.cs` - ObservableGauge cleanup
- `FunkArr.Search/SearchManager.cs` - Failed counter source tag
- `FunkArr.Download.Tests/` - new and updated tests
- All six domain Telemetry.cs files potentially touched for gauge cleanup
