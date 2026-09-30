## Context

FunkArr's community rulesets currently come from a single monolithic JSON at `rundfunkarr/rundfunkarr/main/data/rulesets.json`. The `RuleSetRegistryActor` fetches this URL on a timer, parses the legacy flat format via `CommunityRuleSetParser`, writes individual v2 JSON files to `rulesets/community/`, and reloads the in-memory index. This works but has no versioning, no offline default, and no ability to ship enriched v2-native rulesets (aliases, composite filters, per-rule confidence).

The repo already has release-please for app versioning and `softprops/action-gh-release` patterns from sibling projects (Signal.Bot). The `RuleSetFileWriter` already produces slug-based individual JSON files in v2 format.

## Goals / Non-Goals

**Goals:**
- Ship a versioned, multi-file community ruleset dataset in this repo using the v2 schema
- Independent release-please component so rulesets version separately from the app
- CI packages rulesets into a ZIP attached as a GitHub Release asset
- Runtime refresh via GitHub Releases API with version pinning
- Docker image embeds rulesets for offline out-of-the-box operation
- Port all 59 upstream topics to v2 format (1:1 initially, enrichment in future changes)
- Backward-compatible config migration (legacy URL mode still works)

**Non-Goals:**
- Enriching rulesets with aliases, composite filters, channel filters (future change — this change ports 1:1)
- Web UI for ruleset management
- Automatic ruleset generation changes (existing `RuleSetGeneratorActor` unchanged)
- Changing the local/ or generated/ layer behavior

## Decisions

### 1. Directory structure: `data/community/rulesets/` with version.txt alongside

**Decision:** Rulesets live in `data/community/rulesets/*.json`. Release-please tracks `data/community/` as the component root with `version.txt` and `CHANGELOG.md` at that level.

```
data/
  community/
    version.txt          ← release-please "simple" type
    CHANGELOG.md         ← release-please managed
    rulesets/
      tatort.json
      feuer-und-flamme.json
      heute-show.json
      ...59 files
```

**Rationale:** Keeping `version.txt` and `CHANGELOG.md` one level above `rulesets/` avoids them being included in the ZIP asset. The `rulesets/` subdirectory contains only JSON files, making the ZIP clean.

**Alternative considered:** Flat `data/community/*.json` with version.txt mixed in. Rejected because the ZIP packaging step would need to exclude non-JSON files, adding fragility.

### 2. Release-please component with `(rulesets)` scope

**Decision:** Add a second package in `release-please-config.json` with path `data/community`, component name `community-rulesets`, `include-component-in-tag: true` (producing tags like `community-rulesets-v1.0.0`), and `separate-pull-requests: true`.

Conventional commits use the `(rulesets)` scope: `feat(rulesets): add tatort ruleset`, `fix(rulesets): correct heute-show duration filter`.

**Rationale:** `separate-pull-requests: true` ensures ruleset version bumps don't get mixed into app release PRs. The `(rulesets)` scope is concise and maps clearly to the component. `include-component-in-tag: true` distinguishes ruleset releases from app releases in the GitHub Releases page.

**Alternative considered:** Separate repository for rulesets. Rejected because it fragments the project and complicates the Docker build (would need a fetch step or git submodule).

### 3. ZIP asset via softprops/action-gh-release

**Decision:** After release-please creates a `community-rulesets-*` release, a workflow step zips `data/community/rulesets/*.json` and attaches it using `softprops/action-gh-release` with the existing tag.

```yaml
- name: Package community rulesets
  if: steps.release.outputs['data/community--release_created'] == 'true'
  run: |
    cd data/community/rulesets
    zip -r ../../../community-rulesets.zip *.json

- uses: softprops/action-gh-release@v2
  with:
    tag_name: ${{ steps.release.outputs['data/community--tag_name'] }}
    files: community-rulesets.zip
```

**Rationale:** `softprops/action-gh-release` with an existing tag updates the release without recreating it — proven pattern from Signal.Bot. Simpler than `gh release upload` which requires explicit token handling.

### 4. GitHub Releases API refresh with version pinning

**Decision:** Replace `RuleSetSourceUrl` (string) with three new config options:

| Option | Default | Description |
|---|---|---|
| `RuleSetRepository` | `"st0o0/funkarr"` | GitHub `owner/repo` |
| `RuleSetVersion` | `"latest"` | `"latest"` or pinned version e.g. `"v1.2.0"` |
| `RuleSetRefreshMode` | `"github-release"` | `"github-release"` or `"legacy-url"` for backward compat |

When `RuleSetRefreshMode` is `"github-release"`:
1. `GET https://api.github.com/repos/{owner}/{repo}/releases` (unauthenticated, public repo)
2. Filter releases by tag prefix `community-rulesets-`
3. If pinned: find exact tag `community-rulesets-v{version}`. If latest: take first match.
4. Download the `community-rulesets.zip` asset
5. Extract to `rulesets/community/` (atomic: extract to temp, swap directories)
6. Reload in-memory index

When `RuleSetRefreshMode` is `"legacy-url"`:
- Existing behavior via `RuleSetSourceUrl` + `CommunityRuleSetParser` — unchanged.

**Rationale:** GitHub Releases API is public for public repos, rate-limited at 60 req/hour for unauthenticated access (more than enough for hourly refresh). Version pinning lets users freeze on a known-good ruleset version.

**Alternative considered:** GitHub raw URL for a single bundled JSON. Rejected because it loses the multi-file benefit and requires a separate bundling step that duplicates the source of truth.

### 5. Docker-embedded rulesets as default

**Decision:** The Dockerfile copies `data/community/rulesets/` into the image at `/app/data/rulesets/community/`. This is the default community layer — the refresh mechanism overwrites it with newer versions when available.

```dockerfile
COPY --chown=$APP_UID data/community/rulesets/ /app/data/rulesets/community/
```

**Rationale:** FunkArr works offline out-of-the-box. The embedded rulesets match the app's release version. Users in air-gapped environments get a working community layer without configuration.

### 6. Initial port: 1:1 transformation, no enrichment

**Decision:** The initial dataset is produced by running `CommunityRuleSetParser.Parse()` on the upstream JSON and writing the output via `RuleSetFileWriter`. This produces v2-format files with `all: [...]` filter groups (no OR/NOT), no aliases, no per-rule confidence overrides. Enrichment (aliases, composite filters, channel filters) is a separate future change.

**Rationale:** Shipping 1:1 parity first lets us validate the infrastructure (release-please, CI, refresh mechanism) without conflating it with content changes. Each enrichment can then be a `fix(rulesets):` commit with a clear diff.

### 7. Atomic directory swap on refresh

**Decision:** When extracting a new ZIP, extract to a temporary directory first, then swap: rename `community/` → `community-old/`, rename temp → `community/`, delete `community-old/`. If extraction fails, the old directory remains untouched.

**Rationale:** Prevents a partially-extracted state from corrupting the in-memory index. The `LoadAllFromDisk()` call happens only after a successful swap.

## Risks / Trade-offs

- **GitHub API rate limiting (60 req/hour unauthenticated)** → With hourly refresh, this uses 1 of 60 requests. Risk is negligible. If the repo goes private, auth token would be needed — but that's a non-goal.
- **ZIP extraction adds System.IO.Compression dependency** → Already part of the .NET runtime, no additional NuGet package needed.
- **release-please multi-component output keys** → The output key format `data/community--release_created` uses path with `/` replaced by `--`. This is documented but fragile if the path changes. Mitigated by testing the workflow once before relying on it.
- **Docker image size increase** → 59 small JSON files add ~50-100KB. Negligible compared to the .NET runtime and FFmpeg layers.
- **Stale embedded rulesets** → If a user never refreshes, they run on whatever was bundled at Docker build time. This is acceptable — it's the same situation as any other shipped default config.
- **Breaking config change** → `RuleSetSourceUrl` still works in `legacy-url` mode. Users who've explicitly set it keep working. New installs default to `github-release` mode.

## Open Questions

- Should the refresh check for a newer version before downloading the ZIP (compare local version.txt with remote tag), or always download? Leaning toward version-check first to save bandwidth.
- Should failed refresh attempts use exponential backoff or stick with fixed interval? Current behavior is fixed interval with warning log — keeping that for now.
