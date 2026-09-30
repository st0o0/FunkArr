## ADDED Requirements

### Requirement: RemuxOptions record
The Remuxer SHALL use an internal `RemuxOptions` record to pass parameters to the FFmpeg argument builder.

#### Scenario: RemuxOptions fields
- **WHEN** a `RemuxOptions` is constructed
- **THEN** it SHALL contain `VideoUrl` (string), `SubtitlePath` (string?), `OutputPath` (string), `ProxyUrl` (string?), and `SubtitleLanguage` (string?)

#### Scenario: IsHls computed from URL
- **WHEN** `VideoUrl` ends with `.m3u8` (case-insensitive)
- **THEN** `IsHls` SHALL return `true`

#### Scenario: IsHls false for direct files
- **WHEN** `VideoUrl` ends with `.mp4` or any other extension
- **THEN** `IsHls` SHALL return `false`

### Requirement: RemuxOptions is internal
The `RemuxOptions` record SHALL be `internal sealed` and SHALL NOT be part of any public interface. Only the Remuxer constructs it.

#### Scenario: No public exposure
- **WHEN** an external project references FunkArr.Download
- **THEN** `RemuxOptions` SHALL NOT be visible
