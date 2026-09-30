## Context

`DataPaths` is a singleton constructed during DI setup. It resolves `Complete` and `Incomplete` paths from config. The `DataFiles` service already has `CreateDirectory()` but it's not called for the download root paths at startup.

## Goals / Non-Goals

**Goals:**
- Download directories exist before any health check or download is attempted

**Non-Goals:**
- Creating the data root directory (already handled by Docker VOLUME)
- Permission management (Docker's umask 000 in entrypoint handles this)

## Decisions

### Decision: Create directories in DataPaths constructor via IDataFiles

Inject `IDataFiles` (or `IFileSystem`) into the `DataPaths` construction and call `CreateDirectory` for `Complete` and `Incomplete`. This keeps the responsibility with the path resolver and runs once at startup.

Alternative: Create in Dockerfile. Rejected because the download path is configurable via environment variables, so the Dockerfile can't know the final path.

## Risks / Trade-offs

**[Risk] Constructor side effects** -> Creating directories in a constructor is a side effect. Mitigation: `DataPaths` is a singleton registered in DI, so this runs exactly once. The `DataFiles.CreateDirectory` is already idempotent (calls `Directory.CreateDirectory` which is a no-op if exists).
