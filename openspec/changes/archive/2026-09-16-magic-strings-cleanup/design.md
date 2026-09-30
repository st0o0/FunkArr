## Context

Magic strings are scattered across 10+ files in 7 projects. Most are content types, HTTP headers, and domain-specific string literals that appear in multiple places. The `germanMonths` array manually duplicates German locale data that .NET provides natively. Strategy name strings in MetadataResolver lack a central definition.

## Goals / Non-Goals

**Goals:**
- Centralize all repeated string literals into named constants
- Replace `germanMonths` hand-rolled array with `CultureInfo("de-DE")` parsing
- Make magic strings greppable and refactorable
- Zero behavioral changes — purely mechanical extraction

**Non-Goals:**
- Introducing enums where strings cross serialization boundaries (persistence DTOs, JSON)
- Changing HTTP client registration patterns (that's the resilient-httpclient change)
- Refactoring code structure around the constants
- Touching [FromQuery] Name attributes in ArrApi (those are protocol-spec values)

## Decisions

### Decision 1: Constants location — per-project static classes

Constants SHALL be defined in the project that owns the concept, not in a single mega-constants file:

- `FunkArr.Core/FunkArrHeaders.cs` — `X-FunkArr-Url`, `X-FunkArr-Channel`, etc. (shared across Api and ArrApi)
- `FunkArr.Core/HttpClientNames.cs` — `"MediathekViewWeb"`, `"GitHub"` (shared across host setup and domain projects)
- `FunkArr.Core/MediaTypes.cs` — content type strings used across multiple projects
- `FunkArr.ArrApi/SabnzbdConstants.cs` — `"4.3.3"` version, SABnzbd-specific strings
- `FunkArr.MetadataResolver/ResolutionStrategy.cs` — strategy name constants (`"TitleMatch"`, etc.)

**Why per-project:** Follows the existing project isolation pattern. Constants in `FunkArr.Core` are accessible to all domain projects. Domain-specific constants stay in their domain.

### Decision 2: germanMonths replacement with CultureInfo

Replace the hand-rolled `germanMonths` string array with `CultureInfo("de-DE")` and `DateTime.TryParseExact`. The existing regex for numeric date formats (`dd.MM.yyyy`, `dd.MM.yy`) stays as-is since it works correctly. Only the German month name parsing path changes.

**Pattern:**
```csharp
private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

// Replace manual month lookup with:
DateTime.TryParseExact(dateString, "d. MMMM yyyy", GermanCulture, DateTimeStyles.None, out var date)
```

### Decision 3: Strategy strings stay as string constants, not enum

The resolution strategy strings (`"TitleMatch"`, `"YearMatch"`, etc.) are stored in search result items and may appear in API responses. Converting to an enum would require serialization changes. String constants give us central definition without behavioral change.

### Decision 4: File extension strings are low priority

File extensions like `.mkv`, `.srt`, `.vtt` in `SubtitlePreparer` and `DataPaths` are used in narrow, well-scoped contexts. They'll be extracted alongside nearby constants but don't need a shared constants class — they're domain-specific.

## Risks / Trade-offs

- **[Scope]** This is a large number of small mechanical changes across many files. Risk of typos or missed references mitigated by compilation and existing tests.
- **[CultureInfo locale]** `CultureInfo("de-DE")` is .NET-provided and stable, but culture data comes from ICU on Linux. The German month names have been stable for centuries, so this is effectively zero risk.
