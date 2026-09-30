## Context

FunkArr downloads video from public broadcaster Mediatheken and remuxes to MKV via FFmpeg. Subtitles are provided as URLs in the NZB metadata. Currently, `FfmpegRunner` passes the subtitle URL directly as a second input to FFmpeg (`-i <subtitleUrl>`). All broadcasters deliver subtitles as TTML/EBU-TT-D XML, which FFmpeg cannot demux. A URL-rewriting hack converts ARD and ZDF URLs to WebVTT alternatives, but ORF and SRF have no WebVTT endpoints. The retry-without-subtitles fallback silently drops subtitles for these channels.

The `DownloadWorker` actor currently depends on `IFfmpegRunner` directly and contains subtitle-specific retry logic (detecting subtitle errors from FFmpeg stderr, retrying without subtitles). This mixes subtitle concerns into both the actor and the FFmpeg wrapper.

## Goals / Non-Goals

**Goals:**
- Universal subtitle support for all channels regardless of delivery format
- Clean separation: subtitle preparation, FFmpeg execution, and orchestration are distinct components
- Format detection by content sniffing, not URL pattern matching
- Simplify `FfmpegRunner` and `DownloadWorker` by removing subtitle error handling

**Non-Goals:**
- Supporting subtitle formats beyond TTML/EBU-TT-D, WebVTT, and SRT (no DVD/Blu-ray bitmap subtitles)
- Subtitle language detection (always assumed German)
- Caching or reusing downloaded subtitles across downloads
- Changing the NZB format or search result model

## Decisions

### Decision: Three-component architecture (SubtitlePreparer → Remuxer → FfmpegRunner)

**Choice:** Introduce `ISubtitlePreparer` and `IRemuxer` as separate interfaces, with `IRemuxer` composing the two.

**Alternatives considered:**
- *Inline in FfmpegRunner:* Adds HTTP download and XML parsing to what should be an FFmpeg wrapper. Violates single responsibility.
- *Inline in DownloadWorker:* Actor should not do HTTP calls or XML parsing. Async pre-work before `PipeTo` is awkward in Akka.
- *Single new component replacing FfmpegRunner:* Loses the clean FFmpeg abstraction and makes testing harder.

**Rationale:** Each component has one job. `SubtitlePreparer` handles network + format conversion. `FfmpegRunner` handles FFmpeg process lifecycle. `Remuxer` composes them and manages temp file lifecycle. The worker just calls `IRemuxer.RunAsync()`.

### Decision: Content sniffing for format detection

**Choice:** Detect subtitle format by inspecting the first bytes of the downloaded content, not by URL pattern.

**Rationale:** URL patterns are fragile and channel-specific. Content sniffing is universal:
- Starts with `WEBVTT` → WebVTT
- Starts with `<?xml` or `<tt` → TTML/EBU-TT-D
- Starts with `1\r\n` or `1\n` followed by timestamp → SRT
- Empty or unrecognizable → skip

### Decision: TTML→SRT conversion, not TTML→WebVTT

**Choice:** Convert TTML to SubRip (SRT) format.

**Rationale:** SRT is the simplest text subtitle format — sequential numbered blocks with timestamps and plain text. FFmpeg handles it perfectly with `-c:s srt`. WebVTT would also work but adds no benefit and SRT is more universally supported by media players.

### Decision: Temp subtitle files in the incomplete download directory

**Choice:** Write the converted `.srt` file next to the in-progress MKV in the incomplete directory.

**Rationale:** The incomplete directory is already per-download and gets cleaned up after the download completes (moved to complete or deleted on failure). No separate temp file management needed — the existing lifecycle handles it.

### Decision: IRemuxer replaces IFfmpegRunner in DownloadWorker DI

**Choice:** The `DownloadWorker` constructor takes `IRemuxer` instead of `IFfmpegRunner`. The `IFfmpegRunner` interface remains but becomes an internal dependency of `Remuxer`.

**Rationale:** The worker should not know about FFmpeg details. It needs "take these URLs and make an MKV" — that's `IRemuxer`. The `Remuxer` implementation composes `ISubtitlePreparer` and `IFfmpegRunner` internally.

### Decision: SubtitlePreparer takes HttpClient via DI

**Choice:** `SubtitlePreparer` receives `HttpClient` (via `IHttpClientFactory`) for downloading subtitle content.

**Rationale:** Testable (mock HttpClient), follows .NET conventions, connection pooling handled by the framework.

### Decision: FfmpegRunner parameter rename subtitleUrl → subtitlePath

**Choice:** Rename the parameter to `subtitlePath` and accept only local file paths (or null). Remove all subtitle URL resolution, error detection (`IsSubtitleInputError`), and retry logic from `FfmpegRunner`.

**Rationale:** FfmpegRunner should only wrap FFmpeg process execution. If it receives a subtitle path, it's guaranteed to be a valid local file in a format FFmpeg understands. All validation happened upstream.

## Risks / Trade-offs

- **[TTML dialect variation]** EBU-TT-D-Basic-DE (ARD/ZDF/KiKA) and plain TTML (ORF/SRF) have slightly different XML structures → Mitigation: parse the common `<p begin="..." end="...">` elements that all dialects share, ignore styling/layout attributes
- **[Subtitle download adds latency]** Downloading subtitle content before starting FFmpeg adds a small delay → Mitigation: subtitle files are typically 50-300 KB, download takes <1s even on slow connections; negligible compared to multi-GB video downloads
- **[Empty KiKA responses]** KiKA's subtitle endpoint sometimes returns 200 OK with 0 bytes → Mitigation: content sniffing handles this naturally — empty content returns null, download proceeds without subtitles
- **[No WebVTT fallback]** Removing URL rewriting means ARD/ZDF downloads now use TTML→SRT conversion instead of the server-side WebVTT → Mitigation: our converter is deterministic and testable; removes dependency on undocumented WebVTT endpoints that could disappear
