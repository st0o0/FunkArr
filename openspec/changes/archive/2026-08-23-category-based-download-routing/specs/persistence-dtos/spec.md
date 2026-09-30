## ADDED Requirements

### Requirement: Category field in persistence DTOs
Persistence DTOs for events that carry category information SHALL include a nullable `Category` field with a short JSON key. This applies to `JobEnqueuedDto` (QueueCoordinator), `RequestCreatedDto` (DownloadRequestTracker), and `JobAcceptedDto` (DownloadCoordinator).

#### Scenario: New JobEnqueued event with category serialized
- **WHEN** a `JobEnqueued` domain event with category `"tv"` is converted to DTO
- **THEN** the DTO SHALL include `[JsonProperty("cat")] public string? Category { get; set; }` with value `"tv"`

#### Scenario: Old JobEnqueued event without category deserialized
- **WHEN** a persisted `JobEnqueuedDto` from before this change (no `cat` key in JSON) is deserialized
- **THEN** the `Category` property SHALL be `null` (nullable default)

#### Scenario: RequestCreated event with category
- **WHEN** a `RequestCreated` domain event with category `"movies"` is converted to DTO
- **THEN** the DTO SHALL include the category field with value `"movies"`
