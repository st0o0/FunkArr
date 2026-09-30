## Context

Subtitle handling in FunkArr.Download is a single `SubtitlePreparer` class that downloads, detects format via an if/else cascade (parsing XML twice), and writes a temp file. Format detection, conversion, and emission are entangled. Failures return `null` silently. The `Remuxer` passes `null` to `FfmpegRunner` which skips subtitles without any indication. There is no telemetry, no phase tracking, and no distinction between "no subtitle available" and "subtitle failed."

Research confirms: FFmpeg has no TTML decoder and its VTT decoder breaks on styled content (FFmpeg ticket #8684). Custom parsing is mandatory. Only three formats matter: EBU-TT-D (ARD/ZDF/ORF), WebVTT (BR/SRF/ARTE), SRT (some ORF). Format cannot be predicted by channel (BR delivers VTT, rest of ARD delivers EBU-TT-D). Both pysubs2 and SubtitleEdit (330+ formats, .NET) use the same proven pattern: a single canonical IR with self-contained format classes that handle both detection and parsing.

## Goals / Non-Goals

**Goals:**
- Self-contained format classes: each format handles its own detection + parsing (SubtitleEdit/pysubs2 pattern)
- Single canonical IR: `SubtitleCue(Start, End, Text)` -- all formats normalize to this
- Result type distinguishing success/failure/unavailable
- Subtitle telemetry on existing `FunkArr.Download` meter
- Language metadata flowing to FFmpeg instead of hardcoded `"deu"`

**Non-Goals:**
- Changing `string? SubtitleUrl` on messages/persistence (it's the right type at that boundary)
- Multiple subtitle track support (upstream API limitation)
- Non-German language support (no sources provide it)
- Source/channel-based logic (format determines behavior, not source)
- Post-processing pipeline (HI removal, OCR fixes -- not needed for Mediathek content)
- Format output other than SRT (SRT is the only format FFmpeg reliably muxes into MKV)

## Decisions

### 1. ISubtitleFormat interface: self-contained format classes

**Decision**: Each format is a class implementing `ISubtitleFormat` with two methods:

```csharp
internal interface ISubtitleFormat
{
    string Name { get; }
    bool CanParse(string content);
    List<SubtitleCue> Parse(string content);
}
```

Three implementations: `TtmlFormat`, `WebVttFormat`, `SrtFormat`.

**Why**: SubtitleEdit and pysubs2 both prove this pattern works at scale. Detection and parsing belong together because each format knows best how to identify itself. A separate `SubtitleFormatDetector` class forces detection knowledge away from the format that owns it. The `CanParse` + `Parse` pattern also naturally handles the XDocument caching issue: `TtmlFormat` tries XML parse in `CanParse`, caches the result, reuses it in `Parse`.

**Alternative**: Separate Detector + Parser-per-format (our first proposal). Rejected because it splits knowledge that belongs together and adds a coordination layer with no benefit.

### 2. TtmlFormat caches XDocument between CanParse and Parse

**Decision**: `TtmlFormat` is not static -- it's instantiated per detection attempt. `CanParse` tries `XDocument.Parse`, stores the result in a field, `Parse` reuses it.

```csharp
internal sealed class TtmlFormat : ISubtitleFormat
{
    private XDocument? _cached;
    
    public bool CanParse(string content) { ... _cached = doc; return true; }
    public List<SubtitleCue> Parse(string content) { var doc = _cached ?? XDocument.Parse(content); ... }
}
```

**Why**: Eliminates the double-parse problem from the current code. The instance lifetime is scoped to one subtitle processing operation.

### 3. Format registry loop instead of if/else cascade

**Decision**: `SubtitlePreparer` iterates a list of `ISubtitleFormat` instances:

```csharp
ISubtitleFormat[] formats = [new TtmlFormat(), new WebVttFormat(), new SrtFormat()];

foreach (var format in formats)
{
    if (format.CanParse(content))
        return format.Parse(content);
}
```

**Why**: Extensible (add a format = add a class), testable (each format independently), no detection cascade to maintain. Order matters (TTML first because XML parse is definitive; SRT last because its heuristic is loosest).

### 4. SubtitleResult discriminated union instead of null

**Decision**: `SubtitlePreparer` returns a `SubtitleResult` with three cases:

```
SubtitleResult
├── Succeeded(SubtitleTrack Track, string FilePath)
├── Failed(SubtitleFailureReason Reason, string? Detail)
└── Unavailable
```

**Why**: The current `null` return conflates "no subtitle exists" with "download failed" with "format unknown." The Remuxer and DownloadWorker need to distinguish these for telemetry, logging, and status reporting.

### 5. SrtEmitter as standalone (not part of ISubtitleFormat)

**Decision**: A static `SrtEmitter.Emit(List<SubtitleCue>)` that writes SRT from cues. Not part of `ISubtitleFormat` because we only ever emit SRT.

**Why**: pysubs2/SubtitleEdit have `ToText` on every format because they support writing all formats. We only need SRT output. Adding `Emit` to the interface would force WebVttFormat and TtmlFormat to implement something they'll never use.

### 6. Types live in FunkArr.Download, not FunkArr.Messages

**Decision**: `ISubtitleFormat`, `SubtitleCue`, `SubtitleTrack`, `SubtitleResult` are internal to FunkArr.Download.

**Why**: These are implementation details of the download pipeline. No other domain needs them. The boundary contract (`SubtitleUrl` as string, `HasSubtitles` as bool) stays unchanged.

### 7. BOM handling at the boundary

**Decision**: Strip BOM once at the entry point of `SubtitlePreparer`, before passing content to any format's `CanParse`.

**Why**: Current code strips BOM for detection but passes original content (with BOM) to the converter. Fixing it at the boundary means no format class needs to worry about it.

### 8. Language defaults to "deu", passed as parameter

**Decision**: `SubtitleTrack` carries a `Language` field (ISO 639-2, default `"deu"`). This flows to `FfmpegRunner` which uses it in `-metadata:s:s:0 language={lang}` instead of hardcoded `"deu"`.

**Why**: While all current sources are German, hardcoding the value deep in FFmpeg argument building is wrong. Costs nothing to parameterize.

### 9. Remove DownloadPhase.SubtitleDownload

**Decision**: Remove the dead enum value rather than wiring it up.

**Why**: Subtitle download happens inside `Remuxer.RunAsync` during the `Remux` phase. The telemetry we're adding provides better visibility than a phase enum.

### 10. Telemetry on existing meter

**Decision**: Add to existing `FunkArr.Download` `Telemetry` class:
- `funkarr.download.subtitle_total` (Counter, tags: `status`=succeeded|failed|unavailable, `format`=ttml|vtt|srt|unknown)
- `funkarr.download.subtitle_duration_seconds` (Histogram)

## Risks / Trade-offs

**[WebVTT parser complexity]** Writing a correct VTT parser is non-trivial. We only need timestamp + text extraction, not full spec compliance.
Mitigation: Parse only what we need, ignore styling/positioning/regions. Test against real VTT from BR/SRF.

**[Breaking ISubtitlePreparer]** Changing return type from `Task<string?>` to `Task<SubtitleResult>`.
Mitigation: Only one consumer (Remuxer). Internal interface. Clean break.

**[Instance-based TtmlFormat]** XDocument caching requires instance state, not static methods.
Mitigation: Instances are short-lived (one per subtitle operation), no thread-safety concern.
