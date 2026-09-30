## Why

The current README has em-dashes (violates project convention), references a test project that no longer exists (`FunkArr.Tests`), and is missing major features added since the initial write - the Vue.js UI, MetadataResolver, RuleSet builder, and the full multi-project architecture. It also lacks the one thing *arr users actually need: how to add FunkArr in Prowlarr/Sonarr/Radarr. The UI README is Vite boilerplate.

## What Changes

- Rewrite `README.md` targeting *arr users (not developers)
- Replace all em-dashes with regular dashes
- Add setup instructions for Prowlarr (Newznab indexer) and Sonarr/Radarr (SABnzbd download client)
- Add a Rulesets section explaining what they are and how community sync works
- Update feature list to reflect current state (UI, MetadataResolver, RuleSet builder)
- Fix Build & Test section to show actual test project structure
- Update Configuration section to reference docker-compose.example.yml properly
- Update Alternatives table if needed
- Delete `src/FunkArr.UI/README.md` (Vite boilerplate, not project-specific)

## Capabilities

### New Capabilities

None - this is a documentation-only change.

### Modified Capabilities

None - no spec-level behavior changes.

## Impact

- `README.md` - full rewrite
- `src/FunkArr.UI/README.md` - deleted
- No code changes, no API changes, no dependency changes
