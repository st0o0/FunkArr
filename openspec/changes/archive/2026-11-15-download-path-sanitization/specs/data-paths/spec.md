# Data Paths — Delta Spec

## MODIFIED Requirements

### Requirement: DataPaths resolves download file paths
`DataPaths.ResolveDownload` SHALL sanitize the `title` parameter using `SanitizeFileName` before using it to construct directory and file names. The resolved `incompletePath` and `completePath` SHALL be confined within their respective root directories (`Incomplete` and `Complete`).

#### Scenario: Normal title resolution
- **WHEN** `ResolveDownload` is called with `title: "Tatort.S2026E01.GERMAN.1080p.WEB.h264"`, `entityId: "abc12345-..."`, `category: "show"`, and categories containing `{ Name: "show", Dir: "tv" }`
- **THEN** `CompletePath` SHALL be under the `Complete` root
- **AND** the filename SHALL be `"Tatort.S2026E01.GERMAN.1080p.WEB.h264.mkv"`

#### Scenario: Title with path traversal attempt
- **WHEN** `ResolveDownload` is called with `title: "../../etc/passwd"`
- **THEN** the sanitized title SHALL NOT contain path separators or `..`
- **AND** `CompletePath` SHALL be under the `Complete` root
- **AND** `IncompletePath` SHALL be under the `Incomplete` root

#### Scenario: Title with slashes
- **WHEN** `ResolveDownload` is called with `title: "Show/Season/Episode"`
- **THEN** the sanitized title SHALL replace `/` with `_`
- **AND** the path SHALL remain under the `Complete` root (no extra nesting from the title)

#### Scenario: Empty title after sanitization
- **WHEN** `ResolveDownload` is called with `title: "///"`
- **THEN** the fallback name `"unnamed"` SHALL be used for the directory and filename

### Requirement: Path confinement validation
`ResolveDownload` SHALL validate that the fully resolved `incompletePath` starts with the `Incomplete` directory and the fully resolved `completePath` starts with the `Complete` directory (using `Path.GetFullPath`). If either path escapes its root, `ResolveDownload` SHALL throw `InvalidOperationException`.

#### Scenario: Confinement check passes for normal input
- **WHEN** `ResolveDownload` is called with a clean title
- **THEN** no exception SHALL be thrown
- **AND** the returned paths SHALL start with their respective roots

#### Scenario: Confinement check catches bypass
- **WHEN** a title somehow bypasses sanitization and produces a path outside the download root
- **THEN** `ResolveDownload` SHALL throw `InvalidOperationException` with a message indicating path confinement violation
