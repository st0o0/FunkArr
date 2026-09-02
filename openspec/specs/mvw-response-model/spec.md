# MVW Response Model

## Purpose

Standalone MediathekViewWeb API response models with explicit JSON property naming, contract tests, and updated deserialization options.

## Requirements

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

### Requirement: Contract tests verify MVW API response deserialization

The test suite SHALL include contract tests that deserialize sample MediathekViewWeb API response JSON into the response models.

#### Scenario: Full response deserialization

- **WHEN** a sample JSON response with multiple result items is deserialized
- **THEN** all fields SHALL be correctly mapped including channel, topic, title, description, timestamp, duration, size, url_video, url_video_low, url_video_hd, url_subtitle, url_website

#### Scenario: Empty results deserialization

- **WHEN** a sample JSON response with zero results is deserialized
- **THEN** `Result.Results` SHALL be an empty array and `Result.QueryInfo.TotalResults` SHALL be 0

#### Scenario: Partial fields deserialization

- **WHEN** a sample JSON response item has null url_video_hd and null url_subtitle
- **THEN** the corresponding `MediathekApiItem` properties SHALL be null

#### Scenario: Error response deserialization

- **WHEN** a sample JSON response has `err` set to a non-null string and `result` is null
- **THEN** `MediathekApiResponse.Err` SHALL contain the error string and `Result` SHALL be null

### Requirement: MediathekViewWebManager uses extracted models

The `MediathekViewWebManager` SHALL reference the standalone `MediathekApiModels` types instead of defining its own nested types. The `JsonSerializerOptions` SHALL NOT use `SnakeCaseLower` naming policy — explicit `[JsonPropertyName]` attributes on the models handle naming.

#### Scenario: Deserialization options

- **WHEN** the manager deserializes an API response
- **THEN** the `JsonSerializerOptions` SHALL use `PropertyNameCaseInsensitive = true` as a safety net but SHALL NOT specify a `PropertyNamingPolicy`
