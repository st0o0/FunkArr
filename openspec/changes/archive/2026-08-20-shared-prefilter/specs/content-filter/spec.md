## ADDED Requirements

### Requirement: Accessibility keyword identification
The system SHALL identify a title as an accessibility variant when the title
contains any of the following keywords: `Audiodeskription`,
`Gebärdensprache`, `Gebardensprache`, `klare Sprache`, `Hörfassung`.

#### Scenario: Title contains Audiodeskription
- **WHEN** a title is `"Tatort (Audiodeskription)"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Title contains Gebärdensprache
- **WHEN** a title is `"Tagesschau in Gebärdensprache"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Title contains Gebardensprache without umlaut
- **WHEN** a title is `"Tagesschau in Gebardensprache"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Title contains klare Sprache
- **WHEN** a title is `"Nachrichten in klare Sprache"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Title contains Hörfassung
- **WHEN** a title is `"Hörfilm - Hörfassung"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Title contains no accessibility keyword
- **WHEN** a title is `"Tatort: Ein Mord zuviel"`
- **THEN** the title SHALL NOT be identified as an accessibility variant

### Requirement: Content-type keyword identification
The system SHALL identify a title or topic as a content-type item to skip
when it contains any of the following keywords: `Trailer`, `Vorschau`,
`Teaser`.

#### Scenario: Title contains Trailer
- **WHEN** a title is `"Tatort - Trailer"`
- **THEN** the title SHALL be identified as a content-type item to skip

#### Scenario: Title contains Vorschau
- **WHEN** a title is `"Vorschau auf die nächste Folge"`
- **THEN** the title SHALL be identified as a content-type item to skip

#### Scenario: Topic contains Teaser
- **WHEN** a topic is `"Teaser"` and the title does not contain a
  content-type keyword
- **THEN** the item SHALL be identified as a content-type item to skip

#### Scenario: Title and topic contain no content-type keyword
- **WHEN** a title is `"Tatort: Ein Mord zuviel"` and a topic is `"Tatort"`
- **THEN** the item SHALL NOT be identified as a content-type item to skip

### Requirement: Combined accessibility and content-type filtering
The system SHALL provide a combined check that returns true when a title or
topic matches either an accessibility keyword or a content-type keyword,
checking the title against both keyword sets and the topic against the
content-type keyword set.

#### Scenario: Combined check skips on accessibility keyword in title
- **WHEN** a title is `"Tatort (Audiodeskription)"` and a topic is `"Tatort"`
- **THEN** the combined check SHALL return true

#### Scenario: Combined check skips on content-type keyword in title
- **WHEN** a title is `"Tatort - Trailer"` and a topic is `"Tatort"`
- **THEN** the combined check SHALL return true

#### Scenario: Combined check skips on content-type keyword in topic
- **WHEN** a title is `"Folge 12"` and a topic is `"Vorschau"`
- **THEN** the combined check SHALL return true

#### Scenario: Combined check does not skip on unrelated content
- **WHEN** a title is `"Folge 12"` and a topic is `"Tatort"`
- **THEN** the combined check SHALL return false

### Requirement: Accessibility-only filtering
The system SHALL provide an accessibility-only check that returns true when
a title matches an accessibility keyword, and that ignores content-type
keywords entirely.

#### Scenario: Accessibility-only check ignores content-type keyword
- **WHEN** a title is `"Tatort - Trailer"`
- **THEN** the accessibility-only check SHALL return false

#### Scenario: Accessibility-only check matches accessibility keyword
- **WHEN** a title is `"Tatort (Audiodeskription)"`
- **THEN** the accessibility-only check SHALL return true

#### Scenario: Accessibility-only check does not consider topic
- **WHEN** a title is `"Folge 12"` and a topic is `"Audiodeskription"`
- **THEN** the accessibility-only check SHALL return false

### Requirement: Case-insensitive matching
All keyword checks (accessibility, content-type, combined, and
accessibility-only) SHALL match keywords regardless of the casing used in
the title or topic being checked.

#### Scenario: Lowercase keyword match
- **WHEN** a title is `"tatort audiodeskription"`
- **THEN** the title SHALL be identified as an accessibility variant

#### Scenario: Uppercase keyword match
- **WHEN** a title is `"TATORT TRAILER"`
- **THEN** the title SHALL be identified as a content-type item to skip

#### Scenario: Mixed-case keyword match
- **WHEN** a title is `"Tatort GEBÄRDENSPRACHE Ausgabe"`
- **THEN** the title SHALL be identified as an accessibility variant
