# release-title-format

## Purpose

Carries identification metadata from scoring rules. Scene-style release title formatting has moved to the `scene-release` capability.

## Requirements

### Requirement: MetadataSpec record carries identification results

FunkArr.Messages.Scoring SHALL define a `MetadataSpec` sealed record carrying extracted identification metadata from scoring rules. All fields SHALL be nullable because not every identification strategy produces all three.

#### Scenario: MetadataSpec fields

- **WHEN** a MetadataSpec is created
- **THEN** it SHALL contain `Season` (string?), `Episode` (string?), `AiredAt` (DateTimeOffset?)

#### Scenario: Season and Episode are strings

- **WHEN** a scoring rule extracts season "2024" and episode "27"
- **THEN** MetadataSpec SHALL store them as strings, preserving zero-padding and year-based seasons

#### Scenario: All fields null

- **WHEN** no identification strategy matched
- **THEN** MetadataSpec SHALL be null on ScoredItem (not a MetadataSpec with all-null fields)
