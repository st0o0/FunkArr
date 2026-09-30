## MODIFIED Requirements

### Requirement: ReleaseVariant expands EnrichedItem into quality variants

ReleaseVariant SHALL be a sealed record in FunkArr.Search containing: Title (string), Source (SourceInfo), Identity (MediaIdentity), Score (double), Quality (int), Size (long), Match (MatchInfo?). A static `Expand(EnrichedItem, MediaType mediaType, string? mediaName)` method SHALL produce ReleaseVariant[] by expanding the source URLs into quality variants (HD=1080, normal=720, low=480) and generating a release title via ReleaseTitleBuilder for each variant. The `mediaType` parameter SHALL be a `MediaType` enum value, not a string.

#### Scenario: Item with all three quality URLs

- **WHEN** an EnrichedItem has Source.UrlHd, Source.Url, and Source.UrlLow all set
- **THEN** Expand SHALL return 3 ReleaseVariants with Quality 1080, 720, and 480

#### Scenario: Item with only normal quality URL

- **WHEN** an EnrichedItem has Source.UrlHd=null, Source.Url set, Source.UrlLow=null
- **THEN** Expand SHALL return 1 ReleaseVariant with Quality=720

#### Scenario: Item with no URLs

- **WHEN** an EnrichedItem has all URL fields null
- **THEN** Expand SHALL return an empty array

#### Scenario: Size estimation

- **WHEN** Source.Size is 0 or negative
- **THEN** the ReleaseVariant size SHALL be estimated from Duration and quality-specific bitrate

#### Scenario: MediaType enum replaces string parameter

- **WHEN** `ReleaseVariant.Expand` is called for a TV show
- **THEN** the `mediaType` parameter SHALL be `MediaType.Show`, not `"tv"`
