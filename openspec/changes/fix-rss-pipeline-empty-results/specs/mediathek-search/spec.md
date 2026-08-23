## ADDED Requirements

### Requirement: Explicit JSON property mapping for MediathekViewWeb API
The `MediathekResultItem` record SHALL use `[JsonPropertyName]` attributes on all URL fields to match the MediathekViewWeb API's snake_case JSON keys.

#### Scenario: URL fields are deserialized from snake_case JSON
- **WHEN** the MediathekViewWeb API returns a result with `url_video`, `url_video_hd`, `url_video_low`, `url_subtitle`, and `url_website` keys
- **THEN** the corresponding `Url_Video`, `Url_Video_HD`, `Url_Video_Low`, `Url_Subtitle`, and `Url_Website` properties SHALL be populated with the API values

#### Scenario: Non-URL fields remain unaffected
- **WHEN** the MediathekViewWeb API returns `channel`, `topic`, `title`, `description`, `timestamp`, `duration`, `size`
- **THEN** these fields SHALL continue to deserialize correctly via the existing CamelCase naming policy

### Requirement: Protocol-relative URL normalization
The search pipeline SHALL normalize protocol-relative URLs (`//host/path`) to absolute URLs (`https://host/path`) before they are used in `SearchResult` objects.

#### Scenario: Protocol-relative video URL
- **WHEN** a `MediathekResultItem` has `Url_Video` set to `//tagesschau-progressive.ard-mcdn.de/video.mp4`
- **THEN** the resulting `SearchResult.Url` SHALL be `https://tagesschau-progressive.ard-mcdn.de/video.mp4`

#### Scenario: Already-absolute URL
- **WHEN** a `MediathekResultItem` has `Url_Video` set to `https://example.com/video.mp4`
- **THEN** the resulting `SearchResult.Url` SHALL remain `https://example.com/video.mp4`

#### Scenario: Protocol-relative subtitle URL
- **WHEN** a `MediathekResultItem` has `Url_Subtitle` set to `//utstreaming.zdf.de/subtitles.xml`
- **THEN** the resulting `SearchResult.UrlSubtitle` SHALL be `https://utstreaming.zdf.de/subtitles.xml`
