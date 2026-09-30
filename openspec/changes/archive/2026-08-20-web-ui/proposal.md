## Why

FunkArr currently has no user interface — all interaction happens through machine-to-machine APIs (Newznab, SABnzbd) or log output. Users cannot see download progress, inspect why a ruleset fails to match, or configure arr connections without editing config files manually. A lightweight web UI makes FunkArr accessible to non-technical users and dramatically improves the ruleset authoring experience by providing live feedback against real Mediathek data.

## What Changes

- Add a Vue 3 single-page application served as static files from the .NET host
- Add a setup wizard that guides users through first-run configuration: API key generation, Prowlarr and/or Sonarr/Radarr connection setup with test buttons, path configuration, and system verification
- Add download queue and history views showing real-time progress
- Add a ruleset browser and editor with "test against Mediathek" capability for live rule validation
- Add match intelligence views surfacing data from the existing MatchLedger
- Add new backend API endpoints for rulesets (CRUD + test), config management, setup verification, and clean queue/history JSON
- Add Vite + Tailwind CSS build pipeline in `src/FunkArr.UI/`
- Update Docker build to multi-stage (Node + .NET)
- Support two setup paths: with Prowlarr (indexer managed centrally) and without (Sonarr/Radarr direct)

## Capabilities

### New Capabilities

- `web-ui-shell`: App shell, routing, tab navigation, API client with apikey auth, SPA fallback serving from .NET host
- `setup-wizard`: First-run onboarding flow — API key, Prowlarr/direct mode selection, arr instance connections with test, paths, FFmpeg check, verification summary
- `queue-views`: Download queue and history views consuming clean JSON endpoints (not SABnzbd-wrapped)
- `ruleset-builder`: Browse rulesets with stats, detail view with rules/unmatched items, editor for local overrides and new rulesets, filter group builder, title rule editor, live test against Mediathek
- `match-views`: Match intelligence dashboard — recent matches, topic stats, unmatched item explorer
- `settings-view`: Post-wizard settings form with connection status indicators and re-run wizard option
- `config-api`: Backend endpoints for reading/writing config, testing arr connections, verifying paths/FFmpeg/Mediathek
- `ruleset-api`: Backend endpoints for ruleset CRUD, live testing against Mediathek, reload trigger
- `queue-api`: Clean JSON endpoints for queue and history (parallel to existing SABnzbd-format endpoints)
- `ui-build-pipeline`: Vite + Tailwind build config, dev proxy, Docker multi-stage integration

### Modified Capabilities

- `ruleset-registry`: Needs new messages for listing all rulesets with metadata, getting single rulesets, saving/deleting local overrides via actor messages (currently only supports topic lookup and community refresh)

## Impact

- **New project**: `src/FunkArr.UI/` (Vue 3, Vite, Tailwind, TypeScript)
- **New endpoints**: ~15 new API endpoints across rulesets, config, setup, and queue groups
- **Config model**: `FunkArrOptions` gains `Prowlarr` and `ArrInstances` properties; config persistence moves to `data/config.json` for runtime writes (appsettings.json stays read-only)
- **Actor changes**: `RuleSetRegistryActor` needs additional message handlers for list/get/save/delete/test operations
- **Docker**: Dockerfile becomes multi-stage with Node build step
- **Dependencies**: Node.js/npm added as build-time dependency (not runtime)
- **Static file serving**: ASP.NET pipeline needs `UseStaticFiles` + SPA fallback middleware
