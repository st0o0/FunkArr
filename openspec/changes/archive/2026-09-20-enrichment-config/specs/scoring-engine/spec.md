## MODIFIED Requirements

### Requirement: MetadataSpec includes ConstructedTitle
`MetadataSpec` SHALL include an optional `string? ConstructedTitle` field alongside Season, Episode, and AiredAt. The `ScoringEngine.BuildMetadata` method SHALL preserve the `TracedIdentification.Title` value as `ConstructedTitle` instead of dropping it.

#### Scenario: BuildMetadata preserves constructed title from TitleParts
- **WHEN** a scoring rule with TitleParts strategy matches and `TracedIdentification.Title` is "Roomservice"
- **THEN** the resulting `MetadataSpec.ConstructedTitle` SHALL be "Roomservice"

#### Scenario: BuildMetadata with regex-extracted identification
- **WHEN** a scoring rule with SeasonAndEpisodeNumber strategy matches and `TracedIdentification.Title` is null
- **THEN** the resulting `MetadataSpec.ConstructedTitle` SHALL be null

#### Scenario: BuildMetadata with airdate identification
- **WHEN** a scoring rule with AirdateExtraction strategy matches and `TracedIdentification.Title` is "2026-03-01"
- **THEN** the resulting `MetadataSpec.ConstructedTitle` SHALL be "2026-03-01"

#### Scenario: Existing MetadataSpec callers unaffected
- **WHEN** code constructs `MetadataSpec` without specifying ConstructedTitle
- **THEN** ConstructedTitle SHALL default to null (backwards compatible)
