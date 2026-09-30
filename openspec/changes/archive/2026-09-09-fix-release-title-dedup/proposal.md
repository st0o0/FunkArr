## Why

4 of 10 remaining Sonarr title mismatches are caused by the series name appearing twice in the release title. The Mediathek's `title` field often includes the topic as prefix ("Tatort: Virus") or suffix ("Die dunkle Stunde - Donna Leon"). Since `ReleaseTitleBuilder.Build()` prepends the series name separately, the result doubles up: "Tatort.Tatort.Virus" or "Donna.Leon.2026.Die.dunkle.Stunde...Donna.Leon". Sonarr can't parse the series name from these malformed titles.

Additionally, 2 rulesets have incorrect TVDB mappings in their JSON data, and the `_invalidChars` list strips comma and ampersand from series names which breaks matching for "Elefant, Tiger & Co."

## What Changes

**Code:**
- Add `StripTopicFromTitle(topic, title)` to `ReleaseTitleBuilder` that removes the topic as prefix or suffix from the Mediathek title before building
- Split `Sanitize` into `SanitizeSeries` (lenient - keeps comma) and `SanitizeTitle` (strict - strips all special chars) so series names preserve characters that Sonarr expects

**Data:**
- Fix `unser-sandmaennchen.json`: remove incorrect media block (tvdbId 289772 is "Jan & Henry", not "Unser Sandmannchen")
- Fix `fernsehfilme-und-serien-serien.json`: remove media block (catch-all ruleset, not a specific series)
- Fix `elefant-tiger-und-co.json`: set media.name to "Elefant, Tiger & Co." (with comma and ampersand matching TVDB)

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `release-title-format`: Strip topic prefix/suffix from title, split sanitization for series vs episode parts

## Impact

- **FunkArr.Core**: `ReleaseTitleBuilder.cs` - add StripTopicFromTitle, split Sanitize
- **FunkArr.Search.Tests**: Update test expectations
- **data/community/rulesets**: Fix 3 ruleset JSON files
