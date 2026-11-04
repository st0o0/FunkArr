## MODIFIED Requirements

### Requirement: Enum-to-enum mapping uses exhaustive switch expressions
Conversion between two different enum types SHALL be implemented as a `switch` expression that has one arm per source member and a default arm that throws `ArgumentOutOfRangeException`. Numeric casts such as `(TTarget)(int)source` SHALL NOT be used to convert between enum types. The `Unknown` member, when present in a source enum, SHALL be handled by the `_ => throw` default arm — no explicit mapping arm for `Unknown`.

#### Scenario: Messages to Persistence mapping
- **WHEN** a Messages enum value is converted to its Persistence counterpart
- **THEN** the conversion is a `switch` expression with an explicit arm per non-Unknown member

#### Scenario: Unknown value at mapping boundary
- **WHEN** `Unknown` is passed to an enum mapping extension
- **THEN** the mapper throws `ArgumentOutOfRangeException` via the default arm

#### Scenario: Undefined numeric value
- **WHEN** a source enum value outside the defined members is mapped
- **THEN** the mapper throws `ArgumentOutOfRangeException` carrying the offending value

#### Scenario: New member without mapping
- **WHEN** a member is added to a source enum and no switch arm is added
- **THEN** the build fails (CS8509 as error under warnings-as-errors)
