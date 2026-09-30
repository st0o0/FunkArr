## Why

FunkArr currently fetches community rulesets from an external monolithic JSON file (rundfunkarr/rundfunkarr), transforms it at runtime from the legacy flat format into the v2 model, and writes individual files to disk. This has three problems: (1) the upstream format lacks v2 features (composite filters, aliases, per-rule confidence, channel filtering), so enrichments are impossible without local overrides per show, (2) the upstream dataset has no versioning — a broken push breaks all FunkArr instances on next refresh, and (3) FunkArr cannot function offline without a prior successful fetch. Moving to a FunkArr-native community dataset in this repo — versioned via release-please, distributed as a ZIP release asset, and embedded in the Docker image — gives us schema control, version pinning, atomic updates, and offline defaults.

## What Changes

- Add a `data/community/rulesets/` directory containing one v2 JSON file per topic (59 files), ported from the upstream dataset
- Add a release-please component `community-rulesets` with independent versioning and changelog, triggered by `feat(rulesets):` / `fix(rulesets):` conventional commits
- Add CI workflow step that packages `data/community/rulesets/*.json` into a ZIP and attaches it to the `community-rulesets-vX.Y.Z` GitHub Release via `softprops/action-gh-release`
- Replace `RuleSetSourceUrl` (single JSON URL) with `RuleSetRepository` (GitHub owner/repo) and `RuleSetVersion` ("latest" or pinned version) in `FunkArrOptions`
- Rewrite `RuleSetRegistryActor` refresh to: query GitHub Releases API → download ZIP asset → extract to `community/` → reload
- Embed `data/community/rulesets/` into the Docker image as the default community layer (works offline out-of-the-box)
- Keep `CommunityRuleSetParser` for backward compatibility (users pointing at legacy upstream URLs)

## Capabilities

### New Capabilities
- `community-dataset`: In-repo multi-file community ruleset dataset in v2 schema format, with slug-based filenames and one file per topic
- `ruleset-release-pipeline`: Release-please component for independent community ruleset versioning, CI packaging as ZIP release asset via softprops/action-gh-release
- `github-release-refresh`: Runtime refresh mechanism using GitHub Releases API with version pinning support, replacing the direct URL fetch

### Modified Capabilities
- `ruleset-registry`: Community refresh changes from "fetch single JSON + parse legacy format" to "query GitHub Releases API + download ZIP + extract v2 files". New config options `RuleSetRepository` and `RuleSetVersion` replace `RuleSetSourceUrl`. Backward-compatible fallback to legacy URL mode.
- `ci-infrastructure`: New release-please component `community-rulesets` with `data/community` path, separate PRs, and `(rulesets)` scope. Release workflow gains a ZIP packaging + asset upload step.

## Impact

- **Config**: `RuleSetSourceUrl` deprecated in favor of `RuleSetRepository` + `RuleSetVersion`. Existing configs continue to work (fallback to legacy fetch).
- **Docker**: Dockerfile gains a `COPY` for embedded rulesets. Image size increases by ~50-100KB (59 small JSON files).
- **CI**: `release-please-config.json`, `.release-please-manifest.json`, and `release.yml` all gain the second component.
- **Runtime dependencies**: New dependency on GitHub Releases API (public, no auth required). ZIP extraction via `System.IO.Compression`.
- **Code**: `RuleSetRegistryActor` refresh handler rewritten. `FunkArrOptions` gains new properties. New `GitHubReleaseClient` or similar for API interaction.
- **Backward compat**: `CommunityRuleSetParser` stays for users who want to point at a legacy-format URL.
