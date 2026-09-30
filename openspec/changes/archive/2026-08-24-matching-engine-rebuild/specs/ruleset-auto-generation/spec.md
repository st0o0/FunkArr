## MODIFIED Requirements

### Requirement: Static generator class
The ruleset generator SHALL be a static class `RuleSetGenerator` (not an actor). It SHALL expose a `Generate(MediathekResultItem[] items, int tvdbId, string showName)` method that returns a `RuleSetFile`. The method performs pure CPU work (pattern analysis, regex generation, confidence scoring) and does NOT make any HTTP calls.

#### Scenario: Generate from provided items
- **WHEN** `RuleSetGenerator.Generate(items, 83214, "Tatort")` is called with 200 MediathekResultItems
- **THEN** it SHALL analyze patterns, detect strategy, generate rules, and return a complete `RuleSetFile`

#### Scenario: No HTTP calls
- **WHEN** the generator runs
- **THEN** it SHALL NOT use `MediathekClient` or make any network requests

### Requirement: Channel derivation
The generator SHALL derive channels from the provided items by collecting the distinct `Channel` values of items matching the detected topic. The derived channels SHALL be set on the generated `RuleSetFile.Channels` property.

#### Scenario: Derive channels from items
- **WHEN** items for topic "Tatort" have channels ["Das Erste", "WDR", "NDR", "SWR"]
- **THEN** the generated RuleSetFile SHALL have `Channels = ["Das Erste", "WDR", "NDR", "SWR"]`

#### Scenario: Empty items produce no channels
- **WHEN** no items match the detected topic
- **THEN** generation SHALL fail (no items to analyze)

### Requirement: Title field usage
The generator SHALL always use `field: "title"` for generated TitleRules. The `topicTitle` composite field SHALL NOT be used.

#### Scenario: TitleRules use title field
- **WHEN** the generator creates TitleRules for strategy ItemTitleExact
- **THEN** all TitleRule entries SHALL have `Field = "title"`

#### Scenario: Topic prefix in title
- **WHEN** sample titles contain the topic name as a prefix (e.g., "Tatort: Gefangen")
- **THEN** the generated regex SHALL extract the part after the prefix from the `title` field directly

### Requirement: Mediathek sampling removed
The generator SHALL NOT query the Mediathek API. It receives items as a parameter. The `SampleMediathekAsync` method and `MediathekClient` dependency are removed.

#### Scenario: No MediathekClient dependency
- **WHEN** the generator class is instantiated or called
- **THEN** it SHALL have no constructor parameters and no dependency on `MediathekClient`

### Requirement: RuleSetActor integration
The `RuleSetActor` SHALL handle a `GenerateFromItems(MediathekResultItem[] Items, int TvdbId, string ShowName)` message by calling `RuleSetGenerator.Generate()` synchronously, updating in-memory indexes, and writing the result to disk.

#### Scenario: Generation triggered by TvSearchActor
- **WHEN** `RuleSetActor` receives `GenerateFromItems` with items from a Mediathek query
- **THEN** it SHALL call `RuleSetGenerator.Generate()`, add the result to indexes, and write to the generated/ directory

#### Scenario: Generation failure
- **WHEN** `RuleSetGenerator.Generate()` returns null (no matching topic, no items)
- **THEN** `RuleSetActor` SHALL log a warning and not update indexes

### Requirement: Pattern-based strategy detection
The generator SHALL analyze a sample of Mediathek results to detect the dominant title pattern and select the appropriate matching strategy. Detection thresholds and strategy selection logic are unchanged.

#### Scenario: Season/episode pattern dominates
- **WHEN** 10 out of 15 sampled results contain S##/E## patterns
- **THEN** the system SHALL select the seasonAndEpisodeNumber strategy

#### Scenario: No clear pattern
- **WHEN** no pattern reaches the threshold
- **THEN** the system SHALL select itemTitleIncludes as fallback

### Requirement: Duration filter generation
The generator SHALL derive a duration filter from the provided items. Logic is unchanged (median * 0.5 threshold).

#### Scenario: Duration filter from samples
- **WHEN** the sampled results have durations [44, 45, 44, 45, 43, 44] minutes
- **THEN** the generated filter SHALL be greaterThan with value approximately equal to median * 0.5

### Requirement: Confidence scoring
The generator SHALL validate the generated ruleset against the provided items and compute a confidence score. Logic is unchanged.

#### Scenario: High confidence
- **WHEN** the generated ruleset matches 80%+ of samples
- **THEN** the per-rule confidence SHALL be 0.8 or higher

#### Scenario: Low confidence with fallback
- **WHEN** the generated ruleset matches fewer than 30% of samples
- **THEN** the system SHALL set per-rule confidence to 0.3 and fall back to itemTitleIncludes

## REMOVED Requirements

### Requirement: Mediathek sampling
**Reason**: The generator no longer queries the Mediathek API. Items are provided by the caller (TvSearchActor forwards items it already fetched). This eliminates a duplicate API call that bypassed rate limiting.
**Migration**: Replace `GenerateRuleSet(tvdbId, showName)` with `GenerateFromItems(items, tvdbId, showName)`.

### Requirement: Generated file output
**Reason**: File output is now handled by `RuleSetActor` after calling the static generator. The generator returns a `RuleSetFile` value; the actor writes it to disk.
**Migration**: `RuleSetActor` calls `RuleSetFileWriter.Write()` after generation.
