## Why

Magic strings are scattered across the codebase — content types, HTTP headers, client names, file extensions, custom header keys, and match strategy strings are all hardcoded inline. The hand-rolled `germanMonths` array in MatchMagicActor duplicates what `CultureInfo("de-DE")` provides natively. These make the code harder to grep, refactor, and maintain. Centralizing them into constants and using framework APIs reduces duplication and typo risk.

## What Changes

- Replace `germanMonths` string array in `MatchMagicActor` with `CultureInfo("de-DE")` + `DateTime.TryParseExact` for German date parsing
- Extract content type strings (`"application/xml"`, `"application/x-nzb"`, `"text/plain"`, `"text/event-stream"`, etc.) into constants
- Extract HTTP header names (`"Accept"`, `"User-Agent"`) used in HttpClient setup into constants
- Extract named HttpClient keys (`"MediathekViewWeb"`, `"GitHub"`) into constants
- Extract `X-FunkArr-*` custom header names into shared constants (used in both Newznab and SABnzbd endpoints)
- Extract match strategy strings (`"TitleMatch"`, `"YearMatch"`, `"FuzzyTitleMatch"`, `"AirdateMatch"`, `"RegexExtracted"`) into constants or an enum
- Extract `"none"` strategy check string into a constant (duplicated in TvdbResolverActor and EpisodeResolver)
- Extract hardcoded SABnzbd version `"4.3.3"` into a constant
- Extract file extensions (`.mkv`, `.srt`, `.vtt`) into constants where used alongside other magic strings

## Capabilities

### New Capabilities
- `shared-constants`: Centralized constant definitions for content types, HTTP headers, HttpClient names, custom FunkArr headers, and other cross-cutting string literals

### Modified Capabilities
- `matchmagic-evaluation`: Replace germanMonths with CultureInfo-based date parsing in ItemTitleEqualsAirdate strategy
- `episode-resolution`: Extract match strategy strings into constants
- `sabnzbd-download-api`: Extract version string into a constant

## Impact

- **FunkArr.Core** (or FunkArr.Shared): New constants class(es)
- **FunkArr.MatchMagic**: germanMonths removed, CultureInfo-based parsing in MatchMagicActor
- **FunkArr.MetadataResolver**: Strategy string constants in MovieResolver, EpisodeResolver, TvdbResolverActor
- **FunkArr.Api**: Content type constants in endpoint files
- **FunkArr.ArrApi**: X-FunkArr-* header constants, SABnzbd version constant, content type constants
- **FunkArr.Search**: Content type constant in MediathekViewWebManager
- **FunkArr.RuleSet**: Content type constant in RuleSetApiEndpoints
- **FunkArr (Host)**: HttpClient name constants in setup containers
- **No behavioral changes, no API changes, no breaking changes**
