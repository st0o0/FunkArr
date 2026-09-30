## Context

File path handling in the download pipeline is spread across `DownloadOptions` (category resolution), `DownloadFileService` (path computation + I/O), and `DownloadWorker` (mutable string fields + `ComputePaths()`). Full absolute paths are persisted in `DownloadSucceeded` and `HistoryRecorded` events, which break when `DownloadPath` config changes between runs.

The project is at v0.x, so breaking persistence changes are acceptable without migration.

### .NET ecosystem assessment

Three file system abstraction approaches exist in .NET:
- **`IFileProvider`** (Microsoft built-in) — read-only, no write/move/delete. Designed for static content serving and ASP.NET Core view resolution. Not applicable here.
- **`System.IO.Abstractions`** (community, NuGet) — wraps all of `System.IO` behind `IFileSystem`. Provides `MockFileSystem` for tests. Overkill for this use case — we need 3 operations, not the full surface.
- **Custom domain interfaces** — narrow, domain-scoped interfaces. Microsoft's endorsed pattern when `IFileProvider` doesn't fit (dotnet/runtime#29328 was rejected — no official read/write abstraction planned).

This design uses **custom domain interfaces** (thin `IDownloadFileOperations`) combined with a **value object** (`DownloadPaths` sealed record) following Microsoft's DDD guidance for immutable composite values without identity.

## Goals / Non-Goals

**Goals:**
- Single ownership of path computation in one pure, testable type
- Clean separation between path logic and file system I/O
- Persistence stores only what's needed to recompute paths — no absolute paths in the journal
- Type safety: impossible to confuse temp path with output path

**Non-Goals:**
- Changing the actual directory layout or naming conventions (same output structure)
- Adding path validation or sanitization beyond what exists today
- Changing FFmpeg's interface — it still receives a plain `string outputPath`

## Decisions

### 1. `DownloadPaths` as a sealed record with static factory

```csharp
public sealed record DownloadPaths(
    string IncompletePath,   // full path: {incomplete}/{entityId}/{title}.mkv
    string CompletePath,     // full path: {complete}/[{category}/]{dirName}/{title}.mkv
    string RelativePath)     // relative:  [{category}/]{dirName}/{title}.mkv
{
    public static DownloadPaths Compute(
        string entityId, string title, string? category,
        DownloadOptions options);
}
```

**Why a record instead of a static utility class:** The record bundles the three paths that always travel together. The factory method is the single place where all naming rules live (category resolution, episode identifier check, directory disambiguation). Callers never construct paths ad-hoc.

**Why `RelativePath`:** This is what gets persisted and what history stores. Absolute paths are derived by prepending `options.CompletePath` at read time. This survives config changes, container remounts, and data migrations.

**Alternative considered:** Storing just `entityId + title + category` and recomputing at read time. Rejected because the naming logic (episode identifier regex, `{title}-{entityId[..8]}` disambiguation) could evolve — storing the resolved relative path freezes the output at write time, which is the correct behavior (the file is already on disk with that name).

### 2. `IDownloadFileOperations` replaces `IDownloadFileService`

```csharp
public interface IDownloadFileOperations
{
    void EnsureDirectory(string path);
    void MoveFile(string sourcePath, string destinationPath);
    void DeleteDirectory(string path);
}
```

**Why:** The current `IDownloadFileService` mixes path computation with I/O. The new interface is pure file system operations — no path logic, no `DownloadOptions` dependency. The implementation is trivial (3 one-line methods wrapping `Directory.CreateDirectory`, `File.Move`, `Directory.Delete`).

**Why not `System.IO.Abstractions`:** The library wraps the entire `System.IO` surface behind `IFileSystem` — hundreds of methods across `IFile`, `IDirectory`, `IPath`, etc. We need exactly 3 operations. Adding a NuGet dependency to avoid writing 3 one-line wrappers inverts the cost/benefit. If the project later needs broad file system testability (e.g., RuleSet domain), this decision can be revisited per-domain.

**Why not just use `System.IO` directly in the worker:** Testability. The worker needs to be testable without hitting the file system.

### 3. Use `Path.Join` instead of `Path.Combine`

`DownloadPaths.Compute()` uses `Path.Join` for all path construction instead of `Path.Combine`. `Path.Combine` silently discards all previous segments when a later argument is a rooted path (e.g., `Path.Combine("/downloads", "/evil")` returns `"/evil"`). `Path.Join` always concatenates — it never discards. Since `DownloadPath`, category dirs, and entity IDs come from config or external input, `Path.Join` is the safer choice. The `GetFullPath` call on the final result normalizes separators and resolves relative segments.

### 4. Path derivation moves into `DownloadPaths.Compute()`

All naming rules consolidate here:
- `HasEpisodeIdentifier()` regex (currently in `DownloadFileService`)
- `ResolveCategoryDir()` logic (currently in `DownloadOptions`)
- `{title}-{entityId[..8]}` disambiguation (currently in `DownloadFileService`)
- Incomplete dir: `{incompletePath}/{entityId}/`
- Complete dir: `{completePath}/[{categoryDir}/]{dirName}/`

`DownloadOptions` keeps `CompletePath`, `IncompletePath`, and the `Categories` list as data. It loses `ResolveCategoryDir()` — that logic moves into `DownloadPaths.Compute()`.

### 5. Persistence events drop `FilePath`, gain nothing

`DownloadSucceeded` drops `FilePath`. The worker already has `_state.Title`, `_state.Category`, and `entityId` (from `Context.Self.Path.Name`) — these are the inputs to `DownloadPaths.Compute()`. The `RelativePath` is passed to `RecordDownload` for history storage.

`HistoryRecorded` stores `RelativePath` (relative, not absolute). At query time, `DownloadHistoryManagerState.ToHistoryResult()` can either return the relative path directly or prepend the base path. Since the SABnzbd API needs a file path for display and the UI needs it too, the history query response includes the relative path and the consumer resolves it.

### 6. Worker uses `DownloadPaths?` instead of two `string?` fields

```
- private string? _tempOutputPath;
- private string? _outputPath;
+ private DownloadPaths? _paths;
```

`ComputePaths()` becomes a one-liner: `_paths = DownloadPaths.Compute(entityId, title, category, options)`. References become `_paths!.IncompletePath` and `_paths!.CompletePath`.

### 7. Messages carry `string? RelativePath` instead of `string? FilePath`

- `RecordDownload`: `FilePath` → `RelativePath`
- `WorkerStatusResult`: `FilePath` → `RelativePath`
- `HistoryItem`: `FilePath` → `RelativePath`

The SABnzbd adapter (`DownloadApiEndpoints`) resolves relative → absolute using `DownloadOptions.CompletePath` when building responses.

## Risks / Trade-offs

**[Breaking persistence]** → Acceptable at v0.x per project rules. No migration needed — existing journal data with `FilePath` will simply be ignored on recovery (the field is removed from the record, so deserialization skips it).

**[RelativePath stored at write time vs. recomputed]** → If naming logic changes in the future, old history entries keep the old names (which match the actual files on disk). This is correct behavior — the alternative would require renaming files on disk to match new naming rules.

**[DownloadOptions still has `Categories` list]** → The categories are config data, not logic. `DownloadPaths.Compute()` reads `options.Categories` to resolve category → directory mapping. This is a data dependency, not a logic leak.
