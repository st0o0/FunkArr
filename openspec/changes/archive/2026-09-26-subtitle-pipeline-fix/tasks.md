## 1. SubtitlePreparer format detection rewrite

- [x] 1.1 Replace `StartsWith`-based XML detection with `XDocument.Parse` attempt: if parse succeeds and root element `LocalName` is `tt`, route to `TtmlToSrtConverter`
- [x] 1.2 Keep WEBVTT and SRT detection as fallback after XML parse failure
- [x] 1.3 Add `logger.LogWarning` when `TtmlToSrtConverter.Convert()` returns empty/whitespace despite non-empty input, include the subtitle URL

## 2. TtmlToSrtConverter offset normalization

- [x] 2.1 Add `DetectOffset` method: read `ebuttm:documentStartOfProgramme` from namespace `urn:ebu:tt:metadata` in the document metadata section
- [x] 2.2 Add fallback: if no `documentStartOfProgramme`, compute `min(begin)` across all parsed paragraphs. Treat as offset only if > 30 minutes
- [x] 2.3 Subtract detected offset from all `begin` and `end` timestamps in the SRT generation loop
- [x] 2.4 Clamp all timestamps to `TimeSpan.Zero` minimum (no negative values)

## 3. Telemetry cleanup

- [x] 3.1 Download `Telemetry.cs`: replace ObservableGauge field assignments with static constructor
- [x] 3.2 Enrichment `Telemetry.cs`: rename `_meter` to `Meter` (internal static readonly), move CacheEntries gauge to static constructor, update all references
- [x] 3.3 RuleSet `Telemetry.cs`: replace `Active` ObservableGauge field with static constructor
- [x] 3.4 Search `SearchManager.cs` + `SearchManagerState.cs`: add `Source` to PendingSearch, add `source` tag to Failed and Timeouts counters

## 4. Tests

- [x] 4.1 `TtmlToSrtConverterTests`: add test for 10-hour offset with `documentStartOfProgramme` metadata
- [x] 4.2 `TtmlToSrtConverterTests`: add test for auto-detect offset (no metadata, min begin > 30 min)
- [x] 4.3 `TtmlToSrtConverterTests`: add test for no offset (timestamps start near zero, no metadata)
- [x] 4.4 `TtmlToSrtConverterTests`: add test for 20-hour offset
- [x] 4.5 `TtmlToSrtConverterTests`: add test for timestamp clamping (offset > first begin)
- [x] 4.6 `TtmlToSrtConverterTests`: add test for XML with comment preamble (no `<?xml>` prolog)
- [x] 4.7 SubtitlePreparer XML detection: covered by TtmlToSrtConverter comment preamble test (same XDocument.Parse logic)
- [x] 4.8 Verify all existing `TtmlToSrtConverterTests` still pass (848 tests, 0 failures)

## 5. Verify

- [x] 5.1 `dotnet build src/FunkArr.slnx` (0 errors)
- [x] 5.2 Run all test projects (848 tests, 0 failures)
- [ ] 5.3 `dotnet format src/FunkArr.slnx --verify-no-changes`
