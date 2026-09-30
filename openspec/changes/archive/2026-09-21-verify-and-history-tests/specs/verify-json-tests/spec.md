## ADDED Requirements

### Requirement: Persistence.Tests project exists
A `FunkArr.Persistence.Tests` project SHALL exist in the solution, referencing `FunkArr.Persistence`, `Verify.XunitV3`, and `Newtonsoft.Json`. It MUST use xUnit v3 with Microsoft Testing Platform runner, consistent with other test projects.

#### Scenario: Project builds and runs
- **WHEN** `dotnet run --project src/FunkArr.Persistence.Tests/FunkArr.Persistence.Tests.csproj` is executed
- **THEN** all tests pass

### Requirement: ModuleInitializer disables diff tool
A `ModuleInitializer.cs` SHALL exist in `FunkArr.Persistence.Tests` with `DiffRunner.Disabled = true` to ensure CLI and CI runs fail with diff output instead of launching a diff tool.

#### Scenario: No diff tool launched
- **WHEN** a Verify test fails
- **THEN** the test outputs the diff to stderr, no external diff tool is launched

### Requirement: Verify shape test for every persistence event
Every persistence event record in `FunkArr.Persistence/Events/` SHALL have a Verify shape test that serializes a representative instance with `Newtonsoft.Json` (`Formatting.Indented`) and calls `Verify(json)`. The `.verified.txt` file MUST be committed to source control.

#### Scenario: Download event shape tests
- **WHEN** Verify shape tests are run for Download events
- **THEN** tests exist for: `DownloadEnqueued`, `DownloadDequeued`, `DownloadDispatched`, `DownloadStarted`, `DownloadInitialized`, `DownloadSucceeded`, `DownloadFaulted`, `Download.HistoryRecorded`, `HistoryRemoved`

#### Scenario: ScoringHistory event shape tests
- **WHEN** Verify shape tests are run for ScoringHistory events
- **THEN** tests exist for: `ScoringHistory.HistoryRecorded`

#### Scenario: Shape change detected
- **WHEN** a field is renamed, added, or removed in a persistence event
- **THEN** the Verify test fails with a diff showing the shape change

### Requirement: Verify shape test for persistence snapshot types
Every type used with Akka `SaveSnapshot` SHALL have a Verify shape test.

#### Scenario: PersistedHistoryState shape test
- **WHEN** the Verify shape test runs for `PersistedHistoryState`
- **THEN** it produces a `.verified.txt` file capturing the full JSON structure including nested types

### Requirement: Roundtrip test for every persistence type
Every persistence event and snapshot type SHALL have a roundtrip test that serializes with `Newtonsoft.Json`, deserializes back, and asserts field equality.

#### Scenario: Roundtrip preserves all fields
- **WHEN** a persistence event is serialized and deserialized via Newtonsoft.Json
- **THEN** all field values are preserved

### Requirement: Git configuration for Verify files
`*.received.*` SHALL be in `.gitignore`. `*.verified.txt` SHALL have `text eol=lf working-tree-encoding=UTF-8` in `.gitattributes`.

#### Scenario: Received files not committed
- **WHEN** a Verify test fails and produces a `.received.txt` file
- **THEN** it is ignored by git

#### Scenario: Verified files have consistent line endings
- **WHEN** `.verified.txt` files are committed across different OS platforms
- **THEN** they always use LF line endings
