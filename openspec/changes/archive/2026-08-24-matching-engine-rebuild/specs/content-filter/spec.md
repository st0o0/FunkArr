## MODIFIED Requirements

### Requirement: Combined accessibility and content-type filtering
The system SHALL provide a single `ShouldSkip(title, topic)` method that returns true when a title or topic matches either an accessibility keyword or a content-type keyword. This is the sole filtering method — no separate accessibility-only check exists.

#### Scenario: Combined check skips on accessibility keyword in title
- **WHEN** a title is "Tatort (Audiodeskription)" and a topic is "Tatort"
- **THEN** the combined check SHALL return true

#### Scenario: Combined check skips on content-type keyword in title
- **WHEN** a title is "Tatort - Trailer" and a topic is "Tatort"
- **THEN** the combined check SHALL return true

#### Scenario: Combined check skips on content-type keyword in topic
- **WHEN** a title is "Folge 12" and a topic is "Vorschau"
- **THEN** the combined check SHALL return true

#### Scenario: Combined check does not skip on unrelated content
- **WHEN** a title is "Folge 12" and a topic is "Tatort"
- **THEN** the combined check SHALL return false

## REMOVED Requirements

### Requirement: Accessibility-only filtering
**Reason**: The dual-mode split (ShouldSkip vs ShouldSkipAccessibilityOnly) added complexity without value. ContentFilter runs as a pre-filter — items it removes never reach rule evaluation. All paths now use the unified ShouldSkip method.
**Migration**: Replace all calls to `ShouldSkipAccessibilityOnly` and `IsAccessibilityVariant` with `ShouldSkip(title, topic)`.
