## MODIFIED Requirements

### Requirement: PersistenceMapping aligns with flat FeedEntry
Since FeedEntry and PersistedFeedEntry now have the same flat shape, `ToPersistence()` and `ToDomain()` SHALL map fields directly without traversing a nested SearchResultItem. The mapping SHALL be a straightforward field-to-field copy. The PersistenceMapping class SHALL be `public static` to match its `public static` extension methods — not `internal static` with `public` methods. The `MediaType` conversion SHALL use an exhaustive switch expression instead of a ternary conditional.

#### Scenario: ToPersistence round-trip
- **WHEN** a FeedEntry is converted to PersistedFeedEntry and back
- **THEN** all fields SHALL survive the round-trip with identical values

#### Scenario: No PersistedFeedEntry schema change
- **WHEN** comparing the PersistedFeedEntry record before and after this change
- **THEN** the record SHALL have identical fields and JsonProperty names — no migration needed

#### Scenario: Consistent visibility
- **WHEN** inspecting the PersistenceMapping class
- **THEN** the class SHALL be `public static` and its extension methods SHALL be `public static`

#### Scenario: MediaType uses exhaustive switch
- **WHEN** `ToPersistence()` converts `MediaType` to `PersistedMediaType`
- **THEN** it SHALL use a switch expression with one arm per non-Unknown member
- **AND** `Unknown` SHALL fall through to `_ => throw new ArgumentOutOfRangeException(...)`

#### Scenario: MediaType reverse mapping uses exhaustive switch
- **WHEN** `ToDomain()` converts `PersistedMediaType` to `MediaType`
- **THEN** it SHALL use a switch expression with one arm per non-Unknown member
- **AND** `PersistedMediaType.Unknown` SHALL fall through to `_ => throw new ArgumentOutOfRangeException(...)`
