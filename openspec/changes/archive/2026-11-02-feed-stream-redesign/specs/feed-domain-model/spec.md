## ADDED Requirements

### Requirement: FeedEntry owns its data fields
The `FeedEntry` record in FunkArr.Messages.Feed SHALL have flat, owned fields for all entry data instead of wrapping a `SearchResultItem`. The record SHALL include: StableId, FirstSeenAt, MediaType, Title, Channel, Topic, Url, Duration, Size, Quality, AiredAt, Score, SubtitleUrl, Ids (ExternalIds), Season, and Episode.

#### Scenario: FeedEntry field access
- **WHEN** code accesses a feed entry's title
- **THEN** it SHALL use `entry.Title` directly, not `entry.Item.Title`

#### Scenario: FeedEntry record shape
- **WHEN** a FeedEntry is constructed
- **THEN** it SHALL accept all fields as constructor parameters without requiring a SearchResultItem instance

### Requirement: No Search namespace dependency
FeedEntry SHALL NOT reference `SearchResultItem`, `MatchMetadata`, or any other type from the `FunkArr.Messages.Search` namespace. The Feed domain model SHALL be self-contained within `FunkArr.Messages.Feed` and `FunkArr.Messages.Shared` (for ExternalIds).

#### Scenario: FeedEntry using directives
- **WHEN** inspecting the FeedEntry source file
- **THEN** it SHALL NOT contain `using FunkArr.Messages.Search`

#### Scenario: Architecture test enforcement
- **WHEN** ArchUnitNET tests check FunkArr.Messages.Feed namespace
- **THEN** no type in that namespace SHALL depend on types from FunkArr.Messages.Search

### Requirement: FeedGate constructs flat FeedEntry
`FeedGate.Evaluate` SHALL construct `FeedEntry` records with flat fields extracted from `SearchResultItem` and the corresponding `MediathekItem` source. The mapping from evaluation result fields to FeedEntry fields SHALL happen inside FeedGate.

#### Scenario: FeedEntry from evaluation result
- **WHEN** FeedGate accepts a SearchResultItem with score 0.85 and the source MediathekItem has channel "ARD" and filmlisteTimestamp 1700000000
- **THEN** FeedGate SHALL construct a FeedEntry with Title, Channel, Topic, Url, Duration, Size, Quality, AiredAt, Score=0.85, SubtitleUrl, Ids, Season, and Episode populated from the result and source item, and FirstSeenAt from the filmlisteTimestamp

### Requirement: PersistenceMapping aligns with flat FeedEntry
Since FeedEntry and PersistedFeedEntry now have the same flat shape, `ToPersistence()` and `ToDomain()` SHALL map fields directly without traversing a nested SearchResultItem. The mapping SHALL be a straightforward field-to-field copy.

#### Scenario: ToPersistence round-trip
- **WHEN** a FeedEntry is converted to PersistedFeedEntry and back
- **THEN** all fields SHALL survive the round-trip with identical values

#### Scenario: No PersistedFeedEntry schema change
- **WHEN** comparing the PersistedFeedEntry record before and after this change
- **THEN** the record SHALL have identical fields and JsonProperty names — no migration needed

### Requirement: API consumers adapt to flat field access
FunkArr.Api feed endpoints and FunkArr.ArrApi Newznab feed rendering SHALL access FeedEntry fields directly (e.g., `entry.Title`, `entry.Channel`) instead of through the nested `entry.Item` path.

#### Scenario: Api FeedApiEndpoints adaptation
- **WHEN** the feed API endpoint maps a FeedEntry to a response DTO
- **THEN** it SHALL read `entry.Title`, `entry.Channel`, `entry.Topic`, `entry.Quality`, `entry.Season`, `entry.Episode` directly

#### Scenario: ArrApi NewznabSearchService adaptation
- **WHEN** ToFeedRss maps FeedEntry instances to RSS items
- **THEN** it SHALL pass FeedEntry fields directly to ToItem instead of passing `entry.Item`
