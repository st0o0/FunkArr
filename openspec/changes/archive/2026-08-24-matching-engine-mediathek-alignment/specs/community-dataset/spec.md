## MODIFIED Requirements

### Requirement: Correct field references in lookbehind rules

Community rulesets with TitleRule regex patterns that use lookbehind assertions expecting the topic/show name (e.g., `(?<=Tatort:\s*)`) SHALL reference `field: "topicTitle"` instead of `field: "title"` when the Mediathek does not include the topic prefix in the title field.

#### Scenario: Tatort lookbehind rule
- **WHEN** the Tatort ruleset has a TitleRule with pattern `(?<=Tatort:\s*)\S.*`
- **THEN** the TitleRule SHALL use `field: "topicTitle"` so the regex receives `"Tatort: Die goldene Zeit"` and the lookbehind matches

#### Scenario: Rules without lookbehind unchanged
- **WHEN** a ruleset has TitleRules that only extract from the episode title (e.g., `^(.*?)(?:\s*\([^)]*\))?$`)
- **THEN** the TitleRule SHALL continue using `field: "title"`

#### Scenario: All 19 affected rulesets updated
- **WHEN** a community ruleset contains a lookbehind pattern referencing the show topic
- **THEN** the affected TitleRule's field SHALL be changed from `"title"` to `"topicTitle"`
