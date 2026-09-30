## MODIFIED Requirements

### Requirement: Rule matching

Rule SHALL expose a `Match(MediaItem item, float defaultConfidence)` method that returns a `MatchResult?`. It SHALL first evaluate its FilterGroup against the item. If filters fail, return null. If filters pass, apply the matching strategy. If the strategy produces no identification, return null. If the strategy succeeds, build quality variants from the item's URL fields and return a MatchResult including the extracted `MetadataSpec`.

#### Scenario: Filters pass, strategy succeeds

- **WHEN** a Rule's FilterGroup passes for an item AND the strategy identifies an episode
- **THEN** Match SHALL return a MatchResult with the identification, confidence, quality variants, and a MetadataSpec populated from the identification result

#### Scenario: Filters fail

- **WHEN** a Rule's FilterGroup fails for an item
- **THEN** Match SHALL return null without evaluating the strategy

#### Scenario: Filters pass, strategy fails

- **WHEN** a Rule's FilterGroup passes but the strategy cannot identify an episode (e.g., regex doesn't match)
- **THEN** Match SHALL return null

#### Scenario: MetadataSpec from SeasonAndEpisodeNumber strategy

- **WHEN** a Rule uses SeasonAndEpisodeNumber and extracts Season "01", Episode "05"
- **THEN** the MatchResult's MetadataSpec SHALL have Season "01", Episode "05", AiredAt derived from the item's Timestamp (if > 0)

#### Scenario: MetadataSpec from ItemTitleEqualsAirdate strategy

- **WHEN** a Rule uses ItemTitleEqualsAirdate and extracts date 2024-10-24
- **THEN** the MatchResult's MetadataSpec SHALL have Season null, Episode null, AiredAt 2024-10-24

#### Scenario: MetadataSpec from ByAbsoluteEpisodeNumber strategy

- **WHEN** a Rule uses ByAbsoluteEpisodeNumber and extracts episode "312"
- **THEN** the MatchResult's MetadataSpec SHALL have Season null, Episode "312", AiredAt derived from item Timestamp
