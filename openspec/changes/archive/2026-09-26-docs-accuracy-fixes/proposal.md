## Why

The VitePress documentation has several inaccuracies that silently mislead users:

1. **Wrong config section name**: All four doc pages (DE/EN `configuration.md` and `how-it-works.md`) reference `FunkArr__MatchHistory__*` env vars, but the code binds to `FunkArr:ScoringHistory` (`ScoringHistoryOptions.SectionName`). Users following the docs get silently ignored config.

2. **Comparison page claims no proxy support**: Both DE and EN `comparison.md` say "Proxy-Support: Nein" (line 28) and "Kein Proxy-Support" (line 83). Proxy/route support was added in the `network-routing-and-proxy` change and is extensively documented on the configuration page itself.

3. **`ArrApiOptions` undocumented**: Three config vars (`SearchTimeoutSeconds` default 30, `DownloadTimeoutSeconds` default 10, `SearchCacheTtlSeconds` default 60) exist in `ArrApiOptions.cs` but appear nowhere in the docs.

4. **`DownloadSchedule` undocumented**: `DownloadOptions.DownloadSchedule` (list of `DownloadTimeSlot` with `Start`/`End` TimeOnly) is a real feature with tests and UI exposure but has no configuration reference in the docs.

## What Changes

- Fix `MatchHistory` to `ScoringHistory` in all four affected files (DE/EN configuration + how-it-works)
- Update comparison table: FunkArr Proxy-Support from "Nein" to "Ja (Netzwerk-Routen mit HTTP-Proxy)" / "Yes (network routes with HTTP proxy)"
- Remove the "Kein Proxy-Support" paragraph from FunkArr's weaknesses section in both DE and EN comparison pages
- Add `ArrApi` config section to both DE and EN configuration pages
- Add `DownloadSchedule` config reference to both DE and EN configuration pages

## Capabilities

### New Capabilities

_(none - documentation fixes only)_

### Modified Capabilities

_(none - documentation fixes only)_

## Impact

- 6 markdown files modified (DE: `configuration.md`, `how-it-works.md`, `comparison.md`; EN: same three)
- No code changes
- No new pages, no sidebar changes
