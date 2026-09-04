# Data Files

## Purpose

Unified file I/O service for all domains. Handles directory creation, file moves, reads, writes (including atomic), directory replacement, cleanup, writability checks, and file watching — with transparent Linux permission handling.

## Requirements

### Requirement: IDataFiles interface
The system SHALL define an `IDataFiles` interface in `FunkArr.Core` providing unified file I/O operations for all domains. The interface SHALL handle platform-specific permissions transparently — on Linux, created directories SHALL have mode 777 and created/moved files SHALL have mode 666.

#### Scenario: Interface definition
- **WHEN** the `IDataFiles` interface is inspected
- **THEN** it SHALL declare the following methods:
- **AND** `CreateDirectory(string path)` returning `void`
- **AND** `Remove(string path)` returning `void`
- **AND** `Move(string source, string destination)` returning `void`
- **AND** `ReplaceDirectory(string source, string target)` returning `void`
- **AND** `ReadText(string path)` returning `string`
- **AND** `WriteText(string path, string content)` returning `void`
- **AND** `WriteAtomic(string path, string content)` returning `void`
- **AND** `Exists(string path)` returning `bool`
- **AND** `ListFiles(string directory, string pattern)` returning `string[]`
- **AND** `CanWrite(string directory)` returning `bool`
- **AND** `Watch(string directory, string filter)` returning `IFileSystemWatcher`

### Requirement: DataFiles implementation
The system SHALL provide a `DataFiles` sealed class in `FunkArr.Core` implementing `IDataFiles`, registered as a singleton. The implementation SHALL use `System.IO.Abstractions.IFileSystem` internally for all file system operations.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IDataFiles` SHALL be registered as a singleton backed by `DataFiles`
- **AND** `System.IO.Abstractions.IFileSystem` SHALL be registered as a singleton backed by `FileSystem`

### Requirement: CreateDirectory creates with permissions
`CreateDirectory` SHALL create the directory (and parents) if it does not exist. On Linux, the directory SHALL have mode 777 (rwxrwxrwx). The operation SHALL be idempotent.

#### Scenario: Create new directory on Linux
- **WHEN** `CreateDirectory("/shared/downloads/complete/tv")` is called on Linux
- **THEN** the directory SHALL be created with mode 777

#### Scenario: Create existing directory
- **WHEN** `CreateDirectory` is called for an existing directory
- **THEN** no error SHALL be thrown

### Requirement: Remove handles files and directories safely
`Remove` SHALL delete the target path regardless of whether it is a file or directory. Directory removal SHALL be recursive. If the path does not exist, no error SHALL be thrown. If removal fails (e.g., file locked), the exception SHALL be caught and logged as a warning.

#### Scenario: Remove existing directory
- **WHEN** `Remove("/shared/downloads/incomplete/abc-123")` is called and the directory exists
- **THEN** the directory SHALL be deleted recursively

#### Scenario: Remove existing file
- **WHEN** `Remove("/data/rulesets/local/custom.json")` is called and the file exists
- **THEN** the file SHALL be deleted

#### Scenario: Remove non-existent path
- **WHEN** `Remove("/nonexistent")` is called
- **THEN** no error SHALL be thrown

#### Scenario: Remove failure is non-fatal
- **WHEN** `Remove` fails due to a locked file
- **THEN** the exception SHALL be caught and logged as a warning

### Requirement: Move sets file permissions
`Move` SHALL move a file from source to destination, overwriting if the destination exists. On Linux, the moved file SHALL have mode 666 (rw-rw-rw-). The destination directory MUST already exist.

#### Scenario: Move file with overwrite
- **WHEN** `Move("/tmp/a.mkv", "/out/a.mkv")` is called
- **THEN** the file SHALL be moved, overwriting any existing file at the destination

#### Scenario: Move sets permissions on Linux
- **WHEN** `Move` completes on Linux
- **THEN** the destination file SHALL have mode 666

### Requirement: ReplaceDirectory performs atomic swap
`ReplaceDirectory` SHALL atomically replace the target directory with the source directory. The operation SHALL: rename the existing target to a temporary name, rename the source to the target, then delete the old target. If the swap fails after the old target is renamed away, it SHALL be restored. On Linux, the new target directory SHALL have mode 777.

#### Scenario: Atomic swap success
- **WHEN** `ReplaceDirectory("/tmp/rulesets-new", "/data/rulesets/community")` is called
- **THEN** `/data/rulesets/community` SHALL contain the contents of `/tmp/rulesets-new`
- **AND** the old contents SHALL be deleted

#### Scenario: Swap with no existing target
- **WHEN** `ReplaceDirectory` is called and the target does not exist
- **THEN** the source SHALL simply be renamed to the target

#### Scenario: Swap failure rolls back
- **WHEN** `ReplaceDirectory` fails after renaming the old target away
- **THEN** the old target SHALL be restored to its original path

### Requirement: ReadText reads file content
`ReadText` SHALL return the full text content of the file at the given path.

#### Scenario: Read existing file
- **WHEN** `ReadText("/data/rulesets/community/tatort.json")` is called
- **THEN** the full file content SHALL be returned as a string

### Requirement: WriteText writes with permissions
`WriteText` SHALL write content to the given path, creating the file if it does not exist or overwriting it. On Linux, the file SHALL have mode 666.

#### Scenario: Write new file
- **WHEN** `WriteText("/data/rulesets/version.txt", "1.2.0")` is called
- **THEN** the file SHALL be created with content `"1.2.0"`

#### Scenario: Write sets permissions on Linux
- **WHEN** `WriteText` completes on Linux
- **THEN** the file SHALL have mode 666

### Requirement: WriteAtomic writes via temp file and rename
`WriteAtomic` SHALL write content to a temporary file in the same directory, then rename it to the target path. This ensures no partial writes are visible. On Linux, the file SHALL have mode 666.

#### Scenario: Atomic write produces complete file
- **WHEN** `WriteAtomic("/data/rulesets/local/custom.json", "{...}")` is called
- **THEN** a temporary file SHALL be created in `/data/rulesets/local/`
- **AND** the content SHALL be written to the temporary file
- **AND** the temporary file SHALL be renamed to `custom.json`

#### Scenario: Atomic write failure leaves no partial file
- **WHEN** `WriteAtomic` fails during the write step
- **THEN** the temporary file SHALL be cleaned up
- **AND** the target file SHALL remain unchanged (or absent)

### Requirement: Exists checks files and directories
`Exists` SHALL return `true` if a file or directory exists at the given path.

#### Scenario: File exists
- **WHEN** `Exists("/data/rulesets/community/tatort.json")` is called and the file exists
- **THEN** `true` SHALL be returned

#### Scenario: Directory exists
- **WHEN** `Exists("/data/rulesets/community")` is called and the directory exists
- **THEN** `true` SHALL be returned

#### Scenario: Path does not exist
- **WHEN** `Exists("/nonexistent")` is called
- **THEN** `false` SHALL be returned

### Requirement: ListFiles returns matching files
`ListFiles` SHALL return the full paths of files matching the given pattern in the specified directory. If the directory does not exist, an empty array SHALL be returned.

#### Scenario: List JSON files
- **WHEN** `ListFiles("/data/rulesets/community", "*.json")` is called and 3 JSON files exist
- **THEN** an array of 3 full file paths SHALL be returned

#### Scenario: List from non-existent directory
- **WHEN** `ListFiles("/nonexistent", "*.json")` is called
- **THEN** an empty array SHALL be returned

### Requirement: CanWrite verifies directory writability
`CanWrite` SHALL verify that a directory exists and is writable by creating and immediately deleting a temporary test file. If the directory does not exist or the write test fails, `false` SHALL be returned.

#### Scenario: Writable directory
- **WHEN** `CanWrite("/data")` is called and the directory is writable
- **THEN** `true` SHALL be returned

#### Scenario: Non-writable directory
- **WHEN** `CanWrite("/readonly")` is called and the directory is read-only
- **THEN** `false` SHALL be returned

#### Scenario: Non-existent directory
- **WHEN** `CanWrite("/nonexistent")` is called
- **THEN** `false` SHALL be returned

### Requirement: Watch creates a FileSystemWatcher
`Watch` SHALL return an `IFileSystemWatcher` configured for the given directory and filter pattern. The watcher SHALL monitor `FileName`, `LastWrite`, and `Size` changes. The directory SHALL be created if it does not exist. `EnableRaisingEvents` SHALL be set to `true`.

#### Scenario: Watch existing directory
- **WHEN** `Watch("/data/rulesets/community", "*.json")` is called
- **THEN** an `IFileSystemWatcher` SHALL be returned monitoring that directory for `*.json` changes

#### Scenario: Watch non-existent directory
- **WHEN** `Watch("/data/rulesets/local", "*.json")` is called and the directory does not exist
- **THEN** the directory SHALL be created and an `IFileSystemWatcher` SHALL be returned
