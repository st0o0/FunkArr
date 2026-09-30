## Context

`DownloadWorker` currently inlines all filesystem operations: `Path.Combine`, `Path.GetFullPath`, `Directory.CreateDirectory`, `File.Move`, and `Directory.Delete`. This makes the actor responsible for both download lifecycle management and filesystem plumbing. The `DownloadOptions.Categories` list exists but is never populated, so all downloads land in `complete/{title}/{title}.mkv` without category subdirectories.

The arr ecosystem expects category-based directory routing: Sonarr sends `cat=tv`, Radarr sends `cat=movies`, and the download client sorts files into matching subdirectories under the complete path. SABnzbd's `get_config` endpoint already exposes the categories list — it just returns empty.

## Goals / Non-Goals

**Goals:**
- Extract filesystem operations from `DownloadWorker` into a dedicated `IDownloadFileService`
- Ship default categories (`tv`, `movies`) so downloads route correctly out of the box
- Ensure category subdirectories exist at application startup
- Make path resolution testable without touching the filesystem

**Non-Goals:**
- Runtime category CRUD via API or UI (categories are config-only)
- Centralizing filesystem operations for other domains (RuleSet, etc.)
- Path mapping for remote volume mounts (future concern)

## Decisions

### IDownloadFileService lives in FunkArr.Download

The interface and implementation live in the Download domain project, not Core. It depends on `DownloadOptions` from Core but encapsulates download-specific filesystem knowledge. This keeps Core free of filesystem logic and respects domain isolation.

**Alternative considered**: Placing it in Core as a shared service. Rejected because RuleSet has fundamentally different directory structures and would need its own abstraction — a shared service would be a forced generalization.

### Interface over concrete class for testability

`IDownloadFileService` as an interface injected into `DownloadWorker` allows tests to mock filesystem operations without touching disk. The worker tests can verify it calls the right methods with the right arguments.

### Singleton registration

`DownloadFileService` reads `IOptions<DownloadOptions>` (not `IOptionsMonitor`) — categories don't change at runtime with Option A. Singleton is appropriate since the service is stateless beyond its config dependency.

### Default categories in appsettings.json

Shipping `tv` and `movies` as defaults means FunkArr works correctly with standard Sonarr/Radarr setups without any user configuration. Users can override via environment variables (`FunkArr__Download__Categories__0__Name=...`) for custom setups.

### ApplicationSetupContainer delegates to file service

`EnsureDownloadDirectories` currently creates `complete/` and `incomplete/` directly. It will resolve the file service from DI and call `EnsureDirectories()`, which also creates category subdirectories. This keeps directory creation logic in one place.

## Risks / Trade-offs

**[Config-only categories limit flexibility]** → Acceptable for v0.x. Adding a runtime settings API later doesn't break this design — the file service just gets a different config source.

**[Default categories may not match user's arr setup]** → Users who use custom category names in Sonarr/Radarr need to override via env vars. The setup guide already shows the expected category names.

**[Singleton reads config once]** → If someone changes `appsettings.json` while running, categories won't update until restart. This is fine for Option A — config changes require restart.
