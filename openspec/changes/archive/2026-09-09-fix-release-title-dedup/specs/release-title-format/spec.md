# release-title-format (delta)

## ADDED Requirements

### Requirement: Strip topic prefix/suffix from title

ReleaseTitleBuilder SHALL remove the topic (series name) from the Mediathek title when it appears as a prefix or suffix, to prevent the series name appearing twice in the release title.

#### Scenario: Topic as prefix with colon

- **WHEN** topic is "Tatort" and title is "Tatort: Virus"
- **THEN** the title portion SHALL be "Virus" (stripped "Tatort: " prefix)

#### Scenario: Topic as suffix with dash

- **WHEN** topic is "Donna Leon" and title is "Die dunkle Stunde - Donna Leon"
- **THEN** the title portion SHALL be "Die dunkle Stunde" (stripped " - Donna Leon" suffix)

#### Scenario: Topic as suffix with en-dash

- **WHEN** topic is "Donna Leon" and title is "Die dunkle Stunde - Donna Leon"
- **THEN** the title portion SHALL be "Die dunkle Stunde"

#### Scenario: Title does not contain topic

- **WHEN** topic is "Tatort" and title is "Mord am See"
- **THEN** the title portion SHALL be "Mord am See" (unchanged)

#### Scenario: Stripping would leave empty title

- **WHEN** topic is "Tatort" and title is "Tatort"
- **THEN** the title portion SHALL be "Tatort" (keep original to avoid empty)
