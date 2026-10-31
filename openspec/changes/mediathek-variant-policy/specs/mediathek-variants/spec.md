## Purpose

Detects broadcast variants of Mediathek entries (audio description, sign language, plain language, original version) and applies a configurable policy, so that automatic grabs never pick an unwanted variant while users who need a variant can still get it.

## ADDED Requirements

### Requirement: Variant detection
The system SHALL assign every Mediathek item exactly one variant: `Standard`, `AudioDescription`, `SignLanguage`, `PlainLanguage` or `OriginalVersion`. Detection SHALL use the item's title and topic, case-insensitively and on word boundaries. The description SHALL NOT be used. If several variants match, the first in the order AudioDescription, SignLanguage, PlainLanguage, OriginalVersion SHALL win.

#### Scenario: Audio description
- **WHEN** an item's title contains "Audiodeskription", "Audio-Deskription", "Hörfassung" or the token "AD" in parentheses or after "mit"
- **THEN** its variant SHALL be `AudioDescription`

#### Scenario: Sign language
- **WHEN** an item's title contains "Gebärdensprache" or the token "DGS"
- **THEN** its variant SHALL be `SignLanguage`

#### Scenario: Plain language
- **WHEN** an item's title contains "Klare Sprache", "Leichte Sprache" or "Einfache Sprache", also in the inflected forms "Klarer", "Leichter", "Einfacher"
- **THEN** its variant SHALL be `PlainLanguage`

#### Scenario: Original version
- **WHEN** an item's title contains "Originalversion", "Originalfassung", "Original mit Untertitel" or the token "OV" or "OmU"
- **THEN** its variant SHALL be `OriginalVersion`

#### Scenario: Variant in topic
- **WHEN** an item's topic is "Tatort (mit Gebärdensprache)" and its title has no variant marker
- **THEN** its variant SHALL be `SignLanguage`

#### Scenario: Description is ignored
- **WHEN** an item's description contains "Auch mit Audiodeskription verfügbar" and its title and topic have no variant marker
- **THEN** its variant SHALL be `Standard`

#### Scenario: Token must be a whole word
- **WHEN** an item's title is "Die ADAC-Story" or "OVB Nachrichten"
- **THEN** its variant SHALL be `Standard`

#### Scenario: Several markers
- **WHEN** an item's title contains both "Originalfassung" and "Audiodeskription"
- **THEN** its variant SHALL be `AudioDescription`

### Requirement: Variant policy configuration
The system SHALL read a policy of `Exclude` or `Allow` for each non-standard variant from the configuration section `FunkArr:Variants`. A variant without a configured value SHALL default to `Exclude`. `Standard` SHALL always be allowed. Configuration changes SHALL apply to new searches without a restart.

#### Scenario: Default policy
- **WHEN** no `FunkArr:Variants` section is configured
- **THEN** `AudioDescription`, `SignLanguage`, `PlainLanguage` and `OriginalVersion` SHALL all be `Exclude`

#### Scenario: Allow audio description
- **WHEN** `FunkArr__Variants__AudioDescription=Allow` is configured
- **THEN** `AudioDescription` SHALL be `Allow` and the other non-standard variants SHALL stay `Exclude`

#### Scenario: Invalid value
- **WHEN** a variant is configured with a value other than `Exclude` or `Allow`
- **THEN** startup SHALL fail with a validation error naming the variant

### Requirement: Excluded variants are removed from search results
Items whose variant is excluded by the policy SHALL NOT appear in TV or movie search results. This SHALL apply whether the item was scored, scored without a match, or returned without scoring because no ruleset or matching configuration exists.

#### Scenario: Scored search
- **WHEN** a TV search with a resolved ruleset finds a standard and an audio-description version of the same episode and `AudioDescription` is `Exclude`
- **THEN** only releases of the standard version SHALL be returned

#### Scenario: Search without ruleset
- **WHEN** a search finds items for a topic without a ruleset and one item's variant is `SignLanguage` with policy `Exclude`
- **THEN** that item SHALL NOT be returned

#### Scenario: Allowed variant is returned
- **WHEN** `AudioDescription` is `Allow` and a search finds an audio-description item
- **THEN** releases of that item SHALL be returned

### Requirement: Excluded variants are not scored
Excluded items that reach scoring SHALL NOT be evaluated against ruleset rules. Their item trace SHALL be recorded as not matched, with score 0, no rule traces, and the excluded variant as reason.

#### Scenario: Excluded item in match history
- **WHEN** a scored search contains an audio-description item with policy `Exclude`
- **THEN** the recorded item trace SHALL have Matched=false, Score=0.0, no rule traces and ExcludedVariant=`AudioDescription`

### Requirement: Variant policy is visible in the web UI
The internal API SHALL expose the effective variant policy read-only, and the web UI settings page SHALL display it. The scoring detail view SHALL show the exclusion reason for excluded items.

#### Scenario: Read policy
- **WHEN** `GET /api/system/variants` is requested
- **THEN** the response SHALL list each non-standard variant with its effective policy

#### Scenario: Exclusion shown in scoring detail
- **WHEN** a scoring history entry contains an item excluded as `AudioDescription`
- **THEN** the scoring detail view SHALL label the item as excluded because of audio description
