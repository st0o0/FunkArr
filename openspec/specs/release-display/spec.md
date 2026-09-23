## Purpose

Display-level title resolution for release title construction. Determines the clean MediaName and EpisodeTitle from scoring and enrichment results, separate from the formatting step.

## Requirements

### Requirement: ReleaseDisplay record carries resolved display data
FunkArr.Search SHALL define a `ReleaseDisplay` sealed record with two fields: `MediaName` (string) and `EpisodeTitle` (string). This record represents the fully-resolved display components for a release title, free from raw Mediathek artifacts (topic prefixes, S##E## tags, "Staffel" metadata).

#### Scenario: ReleaseDisplay fields
- **WHEN** a ReleaseDisplay is created
- **THEN** it SHALL contain `MediaName` (string) and `EpisodeTitle` (string)

#### Scenario: MediaName is the series or film name
- **WHEN** a ruleset resolves with mediaName "Tatort"
- **THEN** ReleaseDisplay.MediaName SHALL be "Tatort"

#### Scenario: EpisodeTitle is the clean episode part
- **WHEN** enrichment resolves TVDB episode "König in Gelb" with confidence 0.98
- **THEN** ReleaseDisplay.EpisodeTitle SHALL be "König in Gelb" (no topic prefix, no S##E##, no accessibility suffix)

### Requirement: EnrichedItem carries optional Display
`EnrichedItem` SHALL include a nullable `ReleaseDisplay? Display` field. Items that pass through scoring and/or enrichment SHALL have Display populated. Items without a matching ruleset (unscored) MAY have Display as null until the fallback phase.

#### Scenario: Scored item has Display
- **WHEN** an item matches a scoring ruleset with mediaName "Tatort" and constructedTitle "König in Gelb"
- **THEN** EnrichedItem.Display SHALL be `ReleaseDisplay("Tatort", "König in Gelb")`

#### Scenario: Unscored item has null Display
- **WHEN** an item does not match any scoring ruleset
- **THEN** EnrichedItem.Display SHALL be null

### Requirement: EpisodeTitle resolution follows three-tier priority
The search pipeline SHALL resolve EpisodeTitle using a priority order: TVDB EpisodeName (from enrichment at confidence ≥ 0.9) takes priority over ConstructedTitle (from scoring), which takes priority over the cleaned raw Mediathek title.

#### Scenario: TVDB name used at high confidence
- **WHEN** enrichment resolves with EpisodeName "König in Gelb" and confidence 0.98
- **THEN** Display.EpisodeTitle SHALL be "König in Gelb" (the TVDB name)

#### Scenario: ConstructedTitle used when enrichment confidence is low
- **WHEN** scoring produces ConstructedTitle "Jahrhunderthitze" and enrichment resolves with confidence 0.6
- **THEN** Display.EpisodeTitle SHALL remain "Jahrhunderthitze" (the scoring title, not the TVDB name)

#### Scenario: ConstructedTitle used when no enrichment
- **WHEN** scoring produces ConstructedTitle "vom 18. September 2026" and no enrichment runs
- **THEN** Display.EpisodeTitle SHALL be "vom 18. September 2026"

#### Scenario: Fallback to cleaned raw title
- **WHEN** an item has no ConstructedTitle and no enrichment
- **THEN** Display.EpisodeTitle SHALL be the Mediathek Source.Title with the topic/mediaName prefix and suffix stripped

#### Scenario: Fallback strips topic prefix with various separators
- **WHEN** Source.Title is "Tatort: Virus" and MediaName is "Tatort"
- **THEN** the cleaned title SHALL be "Virus"

#### Scenario: Fallback strips topic suffix
- **WHEN** Source.Title is "Die dunkle Stunde - Donna Leon" and MediaName is "Donna Leon"
- **THEN** the cleaned title SHALL be "Die dunkle Stunde"

### Requirement: MediaName resolution
The search pipeline SHALL set Display.MediaName from the ruleset's `mediaName` field when available, falling back to `Source.Topic` when no ruleset matches or when mediaName is null.

#### Scenario: Ruleset mediaName used
- **WHEN** the matched ruleset has mediaName "Leschs Kosmos"
- **THEN** Display.MediaName SHALL be "Leschs Kosmos"

#### Scenario: Topic used as fallback
- **WHEN** no ruleset matches and Source.Topic is "Terra X"
- **THEN** Display.MediaName SHALL be "Terra X"

### Requirement: Display populated progressively through pipeline phases
The worker state SHALL populate Display fields as each phase completes: MediaName after ruleset resolution, EpisodeTitle after scoring, and EpisodeTitle override after enrichment. Each phase sets only what it knows without clearing prior values.

#### Scenario: After ruleset resolution
- **WHEN** ApplyRuleSet is called with mediaName "Tatort"
- **THEN** the mediaName SHALL be stored for Display construction

#### Scenario: After scoring
- **WHEN** Apply(ScoreCompleted) processes a scored item with constructedTitle "König in Gelb"
- **THEN** the item's Display SHALL have EpisodeTitle "König in Gelb"

#### Scenario: After enrichment overrides
- **WHEN** Apply(EnrichEpisodesCompleted) processes an enriched episode with EpisodeName "König in Gelb" and confidence 0.98
- **THEN** the item's Display.EpisodeTitle SHALL be updated to "König in Gelb" (the TVDB name)

#### Scenario: Enrichment does not override at low confidence
- **WHEN** Apply(EnrichEpisodesCompleted) processes an enriched episode with confidence 0.5
- **THEN** Display.EpisodeTitle SHALL remain unchanged (the scoring title)
