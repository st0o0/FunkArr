## Why

Setting up FunkArr requires configuring Prowlarr (indexer), Sonarr/Radarr (download client), and FunkArr itself (API key, paths, FFmpeg). Users currently have to follow docs manually and troubleshoot connection issues. A read-only setup wizard validates that everything is correctly configured, similar to MediathekArr's setup experience.

## What Changes

- Add a setup wizard page to the web UI that validates Arr app integration
- Validation checks (all read-only, no writes to external systems):
  - FunkArr self-check: API key set, FFmpeg reachable, download path writable, temp path writable
  - Prowlarr check: is FunkArr registered as indexer, is the URL reachable from Prowlarr's perspective
  - Sonarr/Radarr check: is FunkArr registered as download client, connection test
- Wizard is a one-time setup aid — no persistent state, no config actor, no runtime re-checks
- Each check returns pass/fail with actionable guidance on how to fix failures

## Capabilities

### New Capabilities
- `setup-validation`: Read-only validation of FunkArr integration with Arr ecosystem. Self-checks (API key, FFmpeg, paths) and external checks (Prowlarr indexer registration, Sonarr/Radarr download client registration). Returns structured results with fix guidance per check.

### Modified Capabilities

## Impact

- New endpoint group: `/api/setup/validate` (or similar)
- New `FunkArr.Setup/` namespace with validation logic
- Depends on `options-decomposition` for clean access to relevant config sections
- Web UI: new wizard component (part of the web-ui change)
- No changes to existing actors or persistence
