## ADDED Requirements

### Requirement: Solution treats warnings as errors
`src/Directory.Build.props` SHALL set `TreatWarningsAsErrors` to `true` so that every compiler and analyzer warning fails the build for all projects, including test projects.

#### Scenario: Clean build
- **WHEN** `dotnet build src/FunkArr.slnx` is run on the main branch
- **THEN** it completes with zero warnings and zero errors

#### Scenario: New warning introduced
- **WHEN** a change introduces any compiler or analyzer warning
- **THEN** the build fails

#### Scenario: Non-exhaustive switch
- **WHEN** a switch expression over an enum omits a member and has no matching arm
- **THEN** the build fails with CS8509

### Requirement: No blanket suppression of xUnit analyzers
`src/Directory.Build.props` SHALL NOT list `xUnit1051` (or other xUnit analyzer ids) in `NoWarn`.

#### Scenario: NoWarn content
- **WHEN** `Directory.Build.props` is inspected
- **THEN** its `NoWarn` entries do not include `xUnit1051`

### Requirement: Existing warnings resolved before enforcement
All existing `xUnit1051` warnings and the `CS8604` warning in `SabnzbdResponseMapper` SHALL be fixed in code before `TreatWarningsAsErrors` is enabled.

#### Scenario: Null FailMessage
- **WHEN** a SABnzbd history slot is mapped from a download without a failure message
- **THEN** `FailMessage` is an empty string and no nullable warning is emitted

#### Scenario: Enabled last
- **WHEN** the change is implemented in task order
- **THEN** `TreatWarningsAsErrors` is enabled only after the build is warning-free
