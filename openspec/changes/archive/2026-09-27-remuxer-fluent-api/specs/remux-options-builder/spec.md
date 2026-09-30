## ADDED Requirements

### Requirement: RemuxOptions record with fluent builder
RemuxOptions SHALL be a public sealed record with required `VideoUrl` and `OutputPath`, and optional `SubtitleUrl` and `Channel`. Construction SHALL use a static `Create` factory for required fields and `With*` methods for optional fields.

#### Scenario: Create with required fields
- **WHEN** `RemuxOptions.Create("https://example.com/video.mp4", "/output/file.mkv")` is called
- **THEN** it SHALL return a RemuxOptions with VideoUrl and OutputPath set, SubtitleUrl and Channel null

#### Scenario: WithSubtitle adds subtitle URL
- **WHEN** `.WithSubtitle("https://example.com/subs.ttml")` is called on a RemuxOptions
- **THEN** it SHALL return a new RemuxOptions with SubtitleUrl set

#### Scenario: WithChannel adds channel
- **WHEN** `.WithChannel("SRF")` is called on a RemuxOptions
- **THEN** it SHALL return a new RemuxOptions with Channel set

#### Scenario: Fluent chaining
- **WHEN** `RemuxOptions.Create(url, output).WithSubtitle(subUrl).WithChannel("ORF")` is called
- **THEN** it SHALL return a RemuxOptions with all fields set

#### Scenario: Immutability
- **WHEN** `WithSubtitle` or `WithChannel` is called
- **THEN** the original RemuxOptions SHALL remain unchanged

### Requirement: IsHls computed property
RemuxOptions SHALL expose an `IsHls` boolean property computed from the VideoUrl.

#### Scenario: HLS URL
- **WHEN** VideoUrl ends with `.m3u8` (case-insensitive)
- **THEN** IsHls SHALL return true

#### Scenario: Direct MP4 URL
- **WHEN** VideoUrl ends with `.mp4`
- **THEN** IsHls SHALL return false
