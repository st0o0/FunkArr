## RENAMED Requirements

### Requirement: FileService renamed to FileSystemProvider
- **FROM:** `FileService`/`IFileService` in `Shared/`
- **TO:** `FileSystemProvider`/`IFileSystemProvider` in `Providers/FileSystem/`

### Requirement: IFileService renamed to IFileSystemProvider
- **FROM:** `IFileService` interface
- **TO:** `IFileSystemProvider` interface

## MODIFIED Requirements

### Requirement: IFileSystemProvider interface
The system SHALL provide an `IFileSystemProvider` interface (renamed from `IFileService`) in the `Providers/FileSystem/` folder. All file path resolution and I/O operations SHALL go through this provider.

#### Scenario: DI registration
- **WHEN** the application starts
- **THEN** `IFileSystemProvider` SHALL be registered as a singleton with `FileSystemProvider` as the implementation
