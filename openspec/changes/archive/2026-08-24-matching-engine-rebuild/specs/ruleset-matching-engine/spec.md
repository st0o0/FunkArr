## MODIFIED Requirements

### Requirement: Skip keyword filtering
The matching engine SHALL use the unified `ContentFilter.ShouldSkip(title, topic)` to skip items whose title or topic contains accessibility keywords OR content-type keywords (trailers, teasers, previews). This replaces the previous accessibility-only filtering.

#### Scenario: Audiodeskription filtered
- **WHEN** an item title is "Tatort: Die goldene Zeit (Audiodeskription)"
- **THEN** the engine SHALL skip this item

#### Scenario: Gebardensprache filtered
- **WHEN** an item title ends with "(Gebardensprache)" or "(Gebaerdensprache)"
- **THEN** the engine SHALL skip this item

#### Scenario: Trailer filtered
- **WHEN** an item title is "Tatort - Trailer"
- **THEN** the engine SHALL skip this item

#### Scenario: Normal title passes
- **WHEN** an item title is "Tatort: Die goldene Zeit"
- **THEN** the engine SHALL NOT skip this item

### Requirement: Filter evaluation
The matching engine SHALL evaluate the FilterGroup tree for each rule against a Mediathek result item. The `all` group requires all nodes to pass, `any` requires at least one, and `not` requires none to pass. Groups are evaluated recursively. Supported fields for filter evaluation are: `duration`, `title`, `description`, `topic`, `channel`, `timestamp`. The `topicTitle` composite field is NOT supported.

#### Scenario: AND group passes
- **WHEN** a rule has filters `all: [duration > 15, duration < 30]` and the item has duration 1500 seconds (25 minutes)
- **THEN** all filters SHALL pass (25 > 15 AND 25 < 30)

#### Scenario: OR group passes
- **WHEN** a rule has filters `any: [channel eq "ARD", channel eq "Das Erste"]` and the item channel is "Das Erste"
- **THEN** the filter group SHALL pass

#### Scenario: NOT group blocks
- **WHEN** a rule has filters `not: [title contains "Audiodeskription"]` and the item title is "Tatort (Audiodeskription)"
- **THEN** the filter group SHALL fail

#### Scenario: Nested groups
- **WHEN** a rule has filters `all: [duration > 30, any: [channel eq "ARD", channel eq "ZDF"]]` and the item is from ARD with 45min
- **THEN** the filter SHALL pass (45 > 30 AND channel is in [ARD, ZDF])

#### Scenario: Empty filter group passes all
- **WHEN** a rule has an empty FilterGroup (no all, any, or not entries)
- **THEN** all items SHALL pass filter evaluation

#### Scenario: topicTitle field rejected
- **WHEN** a filter references field "topicTitle"
- **THEN** the filter SHALL NOT match (field not recognized, treated as empty string)

## REMOVED Requirements

### Requirement: topicTitle composite field support
**Reason**: The `topicTitle` composite field (`"{topic}: {title}"`) was a workaround for rules that mixed topic and title in regex patterns. With proper field separation, rules SHALL reference actual API fields (`topic`, `title`, `description`, `channel`). Topic validation is done via filters on the `topic` field.
**Migration**: Update all rules using `field: "topicTitle"` to use `field: "title"` with adjusted regex patterns. Use `{ field: "topic", op: "eq", value: "ShowName" }` for topic validation.
