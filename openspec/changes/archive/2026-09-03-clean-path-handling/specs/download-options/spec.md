## REMOVED Requirements

### Requirement: ResolveCategoryDir method
**Reason**: Category resolution logic moves to `DownloadPaths.Compute()` in the `download-path-resolution` capability.
**Migration**: Use `DownloadPaths.Compute()` which handles category resolution internally. `DownloadOptions` retains `Categories` as data — the resolution logic is no longer its responsibility.
