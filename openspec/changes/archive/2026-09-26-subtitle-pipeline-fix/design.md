## Context

The subtitle pipeline has three stages: download (SubtitlePreparer), convert (TtmlToSrtConverter), mux (FfmpegRunner). The download stage works, the mux stage works, but the convert stage is never reached due to format detection failure, and would produce wrong timestamps if it were.

Broadcaster subtitle formats discovered through E2E testing:
- ARD (some content): EBU-TT-D, starts with `<!-- comment -->`, 10h timestamp offset
- ARD (other content): EBU-TT-D, starts with `<?xml>`, no offset
- ZDF: EBU-TT-D, starts with `<?xml>` + `<!-- comment -->`, no offset
- ORF: Plain TTML, starts with `<?xml>`, seconds-only timestamps (`0.00s`)
- SRF: TTML, starts with `<?xml>`, no offset

FFmpeg cannot decode TTML (encode only). Manual conversion to SRT is required.

## Goals / Non-Goals

**Goals:**
- Every subtitle that is downloadable gets muxed into the MKV
- Handle all observed broadcaster formats (ARD, ZDF, ORF, SRF, KiKA, MDR)
- Handle EBU-TT-D timestamp offsets per the spec
- Clean up telemetry inconsistencies

**Non-Goals:**
- Support non-Mediathek subtitle formats (e.g. DVB, PGS)
- FFmpegCore migration or wrapper changes
- Subtitle language auto-detection (always German for DACH broadcasters)

## Decisions

1. **XML detection via parse, not string matching.** `XDocument.Parse` is called first. If it succeeds and the root element's local name is `tt`, route to TTML converter. This handles XML comments, BOMs, processing instructions, whitespace, and namespace prefixes universally. Non-XML content (WEBVTT, SRT) fails the parse, falling through to existing detection.

2. **Offset from `documentStartOfProgramme` metadata, with auto-detect fallback.** The EBU-TT-D spec defines `ebuttm:documentStartOfProgramme` as the programme clock start. We read this first (namespace `urn:ebu:tt:metadata`). If absent, compute `min(begin)` across all paragraphs. Apply offset only when it exceeds 30 minutes (avoids false positives from content that genuinely starts late). Subtract from all begin/end timestamps, clamp to `TimeSpan.Zero`.

3. **30-minute threshold for auto-detect.** A subtitle starting at `00:05:00` is legitimate late-start content. A subtitle starting at `10:00:00` is an offset. 30 minutes is a safe boundary since no broadcast content starts its first subtitle after 30 minutes of silence.

4. **ObservableGauge fields use discard pattern.** `Meter.CreateObservableGauge(...)` registers the callback with the Meter, which holds the reference. The returned `ObservableGauge<T>` object is never used. Replace field assignments with `_ = Meter.CreateObservableGauge(...)` in a static constructor to make intent clear and suppress warnings.

5. **Log warning on empty conversion result.** When `TtmlToSrtConverter.Convert()` returns empty despite non-empty input, `SubtitlePreparer` logs a warning with the URL. This makes silent failures visible without changing behavior.

## Risks / Trade-offs

- **XML parse overhead:** Attempting `XDocument.Parse` on non-XML content (WEBVTT, SRT) will throw and be caught. This is a cold path (once per download) and the content is already in memory, so the overhead is negligible.
- **30-minute threshold:** Could theoretically misclassify content with a legitimate 31-minute gap before first subtitle. This is extremely unlikely for Mediathek content (news, series, documentaries all have subtitles from the start).
- **Namespace brittleness for `documentStartOfProgramme`:** The `urn:ebu:tt:metadata` namespace is standardized. If a broadcaster uses a different namespace for this element, the auto-detect fallback handles it.
