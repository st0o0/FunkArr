# Data Files — Delta Spec

## MODIFIED Requirements

### Requirement: Move validates path confinement
`DataFiles.Move` SHALL validate that both `source` and `destination` resolve (via `Path.GetFullPath`) to paths within the download root directories. If either path is outside the expected root, `Move` SHALL throw `InvalidOperationException`.

#### Scenario: Move within download directories
- **WHEN** `Move` is called with source under `Incomplete` and destination under `Complete`
- **THEN** the move SHALL succeed normally

#### Scenario: Move to path outside download root
- **WHEN** `Move` is called with a destination that resolves outside the download root
- **THEN** `Move` SHALL throw `InvalidOperationException`
- **AND** no file SHALL be moved
