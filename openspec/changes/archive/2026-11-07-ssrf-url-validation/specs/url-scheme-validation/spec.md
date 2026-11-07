## ADDED Requirements

### Requirement: Download pipeline URLs SHALL have an allowed scheme

All URLs that enter the download pipeline through ArrApi (video URLs and subtitle URLs) SHALL be validated to have an `http` or `https` scheme. URLs with any other scheme (including `file`, `ftp`, `gopher`, or no scheme) SHALL be rejected before reaching the domain layer.

#### Scenario: Valid http URL accepted

- **WHEN** an NZB payload contains `Url` with scheme `http`
- **THEN** the URL SHALL pass validation and the download proceeds normally

#### Scenario: Valid https URL accepted

- **WHEN** an NZB payload contains `Url` with scheme `https`
- **THEN** the URL SHALL pass validation and the download proceeds normally

#### Scenario: file:// URL rejected

- **WHEN** an NZB payload contains `Url` with value `file:///etc/passwd`
- **THEN** the URL SHALL be rejected and the payload SHALL be treated as invalid

#### Scenario: ftp:// URL rejected

- **WHEN** an NZB payload contains `Url` with value `ftp://internal/file`
- **THEN** the URL SHALL be rejected and the payload SHALL be treated as invalid

#### Scenario: Malformed URL rejected

- **WHEN** an NZB payload contains `Url` with value `not-a-url`
- **THEN** the URL SHALL be rejected and the payload SHALL be treated as invalid

#### Scenario: Empty or null URL passes through existing checks

- **WHEN** an NZB payload contains a null or empty `Url`
- **THEN** existing null/empty handling SHALL apply (no change to current behavior)

### Requirement: SubtitleUrl SHALL be validated with the same scheme allowlist

The `SubtitleUrl` field on NZB payloads SHALL be validated with the same scheme allowlist as `Url`. A non-allowed subtitle URL SHALL be cleared (set to null) rather than rejecting the entire payload.

#### Scenario: Valid subtitle URL preserved

- **WHEN** an NZB payload contains `SubtitleUrl` with scheme `https`
- **THEN** the subtitle URL SHALL be preserved

#### Scenario: Non-http(s) subtitle URL cleared

- **WHEN** an NZB payload contains `SubtitleUrl` with scheme `file`
- **THEN** the subtitle URL SHALL be set to null
- **THEN** the video download SHALL proceed without subtitles

### Requirement: NZB XML meta URL SHALL be validated

URLs extracted from `<meta type="funkarr-url">` elements in uploaded NZB XML SHALL be validated with the same scheme allowlist. A non-allowed URL SHALL result in the URL being treated as empty.

#### Scenario: Valid meta URL accepted

- **WHEN** an uploaded NZB contains `<meta type="funkarr-url">https://cdn.ard.de/video.mp4</meta>`
- **THEN** the URL SHALL pass validation

#### Scenario: Non-http(s) meta URL rejected

- **WHEN** an uploaded NZB contains `<meta type="funkarr-url">file:///etc/shadow</meta>`
- **THEN** the URL SHALL be treated as empty
- **THEN** the `addfile` request SHALL fail with the existing "missing URL" error path
