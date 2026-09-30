## MODIFIED Requirements

### Requirement: MediathekViewWeb API response models are standalone

The MediathekViewWeb API response models SHALL be defined in a standalone file `MediathekApiModels.cs` in `FunkArr.Search` with `internal` visibility. The models SHALL use explicit `[JsonPropertyName]` attributes matching the actual MediathekViewWeb API field names.

#### Scenario: Response model structure

- **WHEN** a MVW API response is deserialized
- **THEN** the following models SHALL be used: `MediathekApiResponse(MediathekApiResult? Result, string? Err)`, `MediathekApiResult(MediathekApiItem[]? Results, MediathekApiQueryInfo? QueryInfo)`, `MediathekApiQueryInfo(int TotalResults)`, `MediathekApiItem` with all media fields

#### Scenario: Nullable size field

- **WHEN** a MVW API response item has `"size": null`
- **THEN** the `MediathekApiItem.Size` property SHALL be `long?` and deserialize to null without error

#### Scenario: JSON property naming

- **WHEN** `MediathekApiItem` properties are defined
- **THEN** each property SHALL have an explicit `[JsonPropertyName("camelCaseName")]` attribute matching the MVW API response format (e.g., `[JsonPropertyName("url_video_hd")]` for UrlVideoHd, `[JsonPropertyName("url_video_low")]` for UrlVideoLow)

#### Scenario: Testability from test project

- **WHEN** `FunkArr.Search.Tests` needs to test deserialization
- **THEN** the models SHALL be accessible via `[assembly: InternalsVisibleTo("FunkArr.Search.Tests")]`
