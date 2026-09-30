## Context

File I/O is scattered across three domains (Download, RuleSet, Health) using raw `System.IO` calls. Each domain has its own patterns for directory creation, file moves, and error handling. Path computation is fragmented across `FunkArrOptions` (computed properties), `DownloadPaths.Compute()` (static method on a record), and inline `Path.Combine()` in actors. The Download domain recently gained a permissions fix (`DownloadFileOperations`) that the RuleSet domain lacks. Local rulesets (upcoming feature) will need atomic writes — the same pattern `RuleSetUpdater` already implements manually.

`System.IO.Abstractions` is a well-established NuGet package providing a testable `IFileSystem` abstraction with a `MockFileSystem` for in-memory testing. Using it as the foundation avoids reinventing the filesystem abstraction.

## Goals / Non-Goals

**Goals:**
- Single `IDataFiles` interface in Core for all file mutations, with built-in permissions and atomicity
- Single `DataPaths` class in Core for all path resolution from two configurable roots
- Testable file I/O via `MockFileSystem` in all domain test projects
- Consistent Linux permissions on all created files (666) and directories (777)
- Eliminate all direct `System.IO` calls in domain code

**Non-Goals:**
- Async file operations — actors run on Akka dispatcher, sync I/O is acceptable for file operations
- Configurable permissions — 666/777 is sufficient for the Docker multi-container setup
- Abstracting zip extraction — `ZipArchive` stays as standard .NET, only the directory swap is in `IDataFiles`

## Decisions

### D1: IDataFiles in Core, not per-domain interfaces
All domains share the same `IDataFiles` interface rather than domain-specific wrappers (`IRuleSetFileService`, `IDownloadFileService`). The operations are generic enough (create dir, move file, read text) that domain-specific interfaces would be trivially thin with no added value. Domains inject `IDataFiles` directly.

### D2: System.IO.Abstractions as internal dependency
`DataFiles` implementation uses `IFileSystem` from `System.IO.Abstractions` internally. The `IFileSystem` is also registered in DI as a singleton, so test code can provide `MockFileSystem`. Domain code does NOT use `IFileSystem` directly — all access goes through `IDataFiles`.

### D3: DataPaths as a class with computed properties, not an Options class
`DataPaths` is a regular class (not an Options-pattern class) that takes `FunkArrOptions` and `DownloadOptions` in its constructor and computes all absolute paths once. It's registered as a singleton. This avoids recomputing paths on every access and prevents the "options monitor" pattern from leaking path conventions into config.

### D4: Two configurable roots only
- `FunkArr__DataPath` (default: `data`) — internal state (persistence, rulesets, temp)
- `FunkArr__Download__Path` (default: `data/downloads`) — download staging (shared volume in Docker)

Everything below these roots follows a fixed convention. No other path env vars.

### D5: DownloadOptions.Path instead of DownloadPath
The config section is `FunkArr:Download`, so the property is just `Path`. The env var becomes `FunkArr__Download__Path`. This is cleaner than the current `DownloadPath` which stutters with the section name.

### D6: ResolveDownload on DataPaths, not on DownloadOptions
Download path resolution (category dir, episode identifier, entity ID disambiguator) is path logic, not config. It lives on `DataPaths` which already has the `Incomplete`/`Complete` base paths. `DownloadOptions.Categories` is passed as a parameter since `DataPaths` doesn't own category config.

### D7: Permissions via OperatingSystem.IsLinux() guard
`UnixFileMode` APIs are Linux-only. The guard `OperatingSystem.IsLinux()` is recognized by the .NET platform compatibility analyzer (CA1416), so no suppression needed. On Windows/macOS (dev machines, CI), permissions are silently skipped. `MockFileSystem` doesn't support `UnixFileMode`, which is fine — permissions are a deployment concern, not business logic.

### D8: Watch returns IFileSystemWatcher from System.IO.Abstractions
The `Watch` method returns `System.IO.Abstractions.IFileSystemWatcher` (not `System.IO.FileSystemWatcher`). This is testable and the RuleSetManager can use the same event wiring without change. The watcher is created by `IDataFiles`, not by the caller.

## Risks / Trade-offs

### R1: MockFileSystem fidelity
`MockFileSystem` doesn't support `UnixFileMode`, `FileSystemWatcher` events, or cross-volume moves. Permissions testing requires integration tests on Linux. For watchers, `MockFileSystem` provides a mock watcher that can be triggered programmatically — sufficient for unit tests.

### R2: Breaking config change
Renaming `DownloadPath` to `Path` changes the env var from `FunkArr__Download__DownloadPath` to `FunkArr__Download__Path`. Since we're 0.x and the user confirmed breaking changes are fine, this is acceptable. Docker-compose needs updating.

### R3: DataPaths is computed once
If `FunkArrOptions` or `DownloadOptions` change at runtime (via `IOptionsMonitor`), `DataPaths` won't reflect the change. This is intentional — data directory paths should not change while the app is running.
