## REMOVED Requirements

### Requirement: DownloadPaths value object
**Reason**: Absorbed into `DataPaths.ResolveDownload()` in `FunkArr.Core`. The path computation logic (category resolution, episode identifier detection, disambiguator) moves to `DataPaths` where all path resolution is centralized.
**Migration**: Replace `DownloadPaths.Compute(entityId, title, category, options)` with `dataPaths.ResolveDownload(entityId, title, category, options.Categories)`. The returned `ResolvedDownload` record has the same three properties: `IncompletePath`, `CompletePath`, `RelativePath`.

### Requirement: DownloadPaths.Compute factory method
**Reason**: Absorbed into `DataPaths.ResolveDownload()`. All path computation logic including `Path.Join` usage and `Path.GetFullPath` normalization is preserved in the new location.
**Migration**: Replace all calls to `DownloadPaths.Compute()` with `dataPaths.ResolveDownload()`.

### Requirement: Category resolution logic
**Reason**: Moved to `DataPaths.ResolveDownload()` — identical behavior, new location.
**Migration**: No behavioral change. Category resolution (case-insensitive matching, Dir fallback to Name) is preserved in `DataPaths`.

### Requirement: Episode identifier detection
**Reason**: Moved to `DataPaths` — identical regex patterns (`S\d{2,}E\d{2,}`, `.E\d{2,}.`, `\d{4}-\d{2}-\d{2}`), same case-insensitive behavior.
**Migration**: No behavioral change. Detection logic is preserved in `DataPaths`.
