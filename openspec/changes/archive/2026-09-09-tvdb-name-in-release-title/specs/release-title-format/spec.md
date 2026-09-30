# release-title-format (delta)

## MODIFIED Requirements

### Requirement: ReleaseTitleBuilder formats scene-style titles

FunkArr.Core SHALL define a `ReleaseTitleBuilder` static class with a `Build` method that produces scene-style release titles from media metadata. The series name portion of the title SHALL use the TVDB media name from the ruleset when available, falling back to the Mediathek topic name.

#### Scenario: TVDB media name used when available

- **WHEN** a ruleset has `media.name: "Löwenzahn"` and the Mediathek topic is "Löwenzahn mit Peter Lustig"
- **THEN** the release title SHALL start with `Löwenzahn` (not `Löwenzahn.mit.Peter.Lustig`)

#### Scenario: Fallback to topic when no media name

- **WHEN** a ruleset has no `media.name` configured
- **THEN** the release title SHALL use the Mediathek topic as before
