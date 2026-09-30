## MODIFIED Requirements

### Requirement: Date fields use DateTimeOffset
`DownloadHistoryItem.CompletedAt` SHALL be `DateTimeOffset` instead of `string`. `RuleSetListEntry.LastScoringRun` SHALL be `DateTimeOffset?` instead of `string?`. No manual `.ToString("o")` conversion SHALL occur in endpoint code.

#### Scenario: History item has DateTimeOffset
- **WHEN** the API returns a download history item
- **THEN** `completedAt` is serialized as an ISO 8601 DateTimeOffset by System.Text.Json

#### Scenario: No manual date formatting in endpoints
- **WHEN** endpoint code is inspected
- **THEN** no `.ToString("o")` calls exist for date fields

### Requirement: Unified registry access
All endpoint classes SHALL use `await registry.GetAsync<T>()` for actor resolution. No synchronous `registry.Get<T>()` calls SHALL exist in endpoint code.

#### Scenario: Downloads endpoints use GetAsync
- **WHEN** `DownloadsApiEndpoints.cs` is inspected
- **THEN** all actor resolution uses `await registry.GetAsync<T>()`
