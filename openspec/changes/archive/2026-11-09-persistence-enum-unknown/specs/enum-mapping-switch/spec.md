## MODIFIED Requirements

### Requirement: Enum-to-enum mapping uses exhaustive switch expressions
Conversion between two different enum types SHALL be implemented as a `switch` expression that has one arm per source member and a default arm that throws `ArgumentOutOfRangeException`. Numeric casts such as `(TTarget)(int)source` SHALL NOT be used to convert between enum types. The `Unknown` member, when present in a source enum, SHALL be handled by the `_ => throw` default arm — no explicit mapping arm for `Unknown`. This applies to all Persistence enums now that they have `Unknown = 0`.

#### Scenario: Messages to Persistence mapping
- **WHEN** a Messages enum value is converted to its Persistence counterpart
- **THEN** the conversion is a `switch` expression with an explicit arm per non-Unknown member

#### Scenario: Persistence to Messages mapping
- **WHEN** a Persistence enum value is converted to its Messages counterpart
- **THEN** the conversion is a `switch` expression with an explicit arm per non-Unknown member
- **AND** `PersistedXxx.Unknown` falls through to the `_ => throw` default arm

#### Scenario: Unknown value at mapping boundary
- **WHEN** `Unknown` is passed to an enum mapping extension (from either side)
- **THEN** the mapper throws `ArgumentOutOfRangeException` via the default arm

#### Scenario: DownloadPriority mapping updated
- **WHEN** `DownloadPriority` is mapped to `PersistedDownloadPriority` or vice versa
- **THEN** the switch uses the new sequential values (`Paused = 1, Low = 2, Normal = 3, High = 4`)
- **AND** `Unknown` from either side throws via the default arm

#### Scenario: New member without mapping
- **WHEN** a member is added to a source enum and no switch arm is added
- **THEN** the build fails (CS8509 as error under warnings-as-errors)
