# enum-mapping-switch Specification

## Purpose

Enum-to-enum conversions use exhaustive switch expressions with a throwing default instead of numeric casts.

## Requirements

### Requirement: Enum-to-enum mapping uses exhaustive switch expressions
Conversion between two different enum types SHALL be implemented as a `switch` expression that has one arm per source member and a default arm that throws `ArgumentOutOfRangeException`. Numeric casts such as `(TTarget)(int)source` SHALL NOT be used to convert between enum types.

#### Scenario: Messages to Persistence mapping
- **WHEN** a Messages enum value is converted to its Persistence counterpart
- **THEN** the conversion is a `switch` expression with an explicit arm per member

#### Scenario: Undefined numeric value
- **WHEN** a source enum value outside the defined members is mapped
- **THEN** the mapper throws `ArgumentOutOfRangeException` carrying the offending value

#### Scenario: New member without mapping
- **WHEN** a member is added to a source enum and no switch arm is added
- **THEN** the build fails (CS8509 as error under warnings-as-errors)

### Requirement: Existing cast-based mappings are converted
The following conversions SHALL use switch expressions: all Messages <-> Persistence enum conversions in `FunkArr.History`, the `EnrichmentMethod` and `RuntimeMode` conversions in `FunkArr.Api` rule set mapping, the `DownloadPriority` conversions in `FunkArr.Api` download endpoints, and the SABnzbd priority integer conversion in `FunkArr.ArrApi`.

#### Scenario: History mapping round trip
- **WHEN** a trace containing every enum member is mapped to persistence and back
- **THEN** each enum value round-trips unchanged

#### Scenario: Sabnzbd priority input
- **WHEN** the SABnzbd priority integer is `-1`, `0`, or `1`
- **THEN** it maps to `Low`, `Normal`, or `High` respectively

#### Scenario: Sabnzbd priority out of range
- **WHEN** the SABnzbd priority integer is outside `-1..1`
- **THEN** it is clamped to the nearest bound before mapping, matching the previous behavior

### Requirement: Nullable enums map through null-check
Nullable enum values SHALL be mapped with a null-check that applies the switch-based helper to the non-null value and yields `null` otherwise.

#### Scenario: Null excluded variant
- **WHEN** a trace has `ExcludedVariant = null`
- **THEN** the mapped persistence value is `null`
