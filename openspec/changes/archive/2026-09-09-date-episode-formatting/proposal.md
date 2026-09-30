## Why

13 of 39 Sonarr import warnings were "Invalid season or episode" - Sonarr can't parse valid episode identification from the release title. This affects three patterns:
1. **Date-based shows** (e.g., "Bibi.Blocksberg.2026-09-05.Title") - Sonarr's Daily type needs the date right after the series name, but FunkArr puts the topic first, then the date
2. **Absolute episodes** (e.g., "Schloss.Einstein.1075.Title", "Sturm.der.Liebe.E1666") - Sonarr can handle absolute numbering but only in specific formats
3. **Year-only** (e.g., "Polizeiruf.110.2026.Title") - Sonarr interprets the year as a season with no episode

The root cause is that when episode resolution doesn't produce S##E## metadata, the release title falls through to less Sonarr-compatible formats.

## What Changes

- When metadata has only AiredAt (no season/episode), ensure the date is placed immediately after the topic in YYYY-MM-DD format for Sonarr Daily type compatibility
- When metadata has only an absolute episode number (no season), format as S01E### to maximize Sonarr compatibility (Sonarr maps absolute episodes to S01)
- When metadata has neither season, episode, nor airdate, include the Mediathek timestamp as a date fallback if available

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `release-title-format`: Improve TV identifier formatting for date-based and absolute episode patterns to maximize Sonarr parsing compatibility

## Impact

- **FunkArr.Core**: `ReleaseTitleBuilder.cs` - refine `AppendTvIdentifier()` logic
- **FunkArr.Search.Tests**: Update test expectations for date and absolute episode formats
- No API contract changes - the title format is an internal detail
