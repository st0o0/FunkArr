## 1. Community Dataset

- [x] 1.1 Create `data/community/rulesets/` directory structure
- [x] 1.2 Write a one-off porting script/tool that fetches upstream `rulesets.json`, runs `CommunityRuleSetParser.Parse()`, and writes v2 JSON files via `RuleSetFileWriter.WriteAll()` to `data/community/rulesets/`
- [x] 1.3 Run the porting tool and commit all 59 generated JSON files
- [x] 1.4 Validate all generated files deserialize cleanly into `RuleSetFile` (unit test or script)
- [x] 1.5 Create `data/community/version.txt` with initial version `0.1.0`

## 2. Release-Please Component

- [x] 2.1 Add `data/community` package to `release-please-config.json` with component `community-rulesets`, release-type `simple`, `include-component-in-tag: true`, `separate-pull-requests: true`, and matching `changelog-sections`
- [x] 2.2 Add `data/community` entry to `.release-please-manifest.json` with version `0.1.0`

## 3. CI Workflow

- [x] 3.1 Add ruleset ZIP packaging step to `release.yml` — zip `data/community/rulesets/*.json` when `data/community--release_created` is true
- [x] 3.2 Add `softprops/action-gh-release@v2` step to attach `community-rulesets.zip` to the community-rulesets release tag
- [x] 3.3 Expose community-rulesets release outputs from the release-please step (tag_name, version)

## 4. Configuration

- [x] 4.1 Add `RuleSetRepository`, `RuleSetVersion`, and `RuleSetRefreshMode` properties to `FunkArrOptions`
- [x] 4.2 Update `appsettings.json` with new defaults (`RuleSetRepository: "st0o0/funkarr"`, `RuleSetVersion: "latest"`, `RuleSetRefreshMode: "github-release"`)
- [x] 4.3 Keep `RuleSetSourceUrl` property for backward compatibility (used in `legacy-url` mode)

## 5. GitHub Release Refresh

- [x] 5.1 Create `GitHubReleaseClient` service — queries GitHub Releases API, finds release by tag prefix and version, downloads ZIP asset
- [x] 5.2 Implement version check: read local `version.txt`, compare with remote release tag, skip download if matching
- [x] 5.3 Implement atomic extraction: extract ZIP to temp directory, swap with existing `community/`, write `version.txt` with new version
- [x] 5.4 Register `GitHubReleaseClient` in DI (`FunkArrServiceSetup`)
- [x] 5.5 Set `User-Agent: FunkArr/{version}` header on all GitHub API requests

## 6. RuleSetRegistryActor Refresh Rewrite

- [x] 6.1 Refactor `HandleRefreshCommunityAsync` to branch on `RuleSetRefreshMode`: `github-release` uses `GitHubReleaseClient`, `legacy-url` uses existing `CommunityRuleSetParser` flow
- [x] 6.2 In `github-release` mode: call `GitHubReleaseClient` to check version, download ZIP if newer, extract atomically, then `LoadAllFromDisk()`
- [x] 6.3 Log refresh outcome: version updated (Info), version unchanged (Debug), error (Warning)

## 7. Docker

- [x] 7.1 Add `COPY data/community/rulesets/ /app/data/rulesets/community/` to Dockerfile (before the existing `COPY --chown=$APP_UID . .` for the publish output)
- [x] 7.2 Verify Dockerfile build context — `data/community/rulesets/` must be accessible from the repo root build context (adjust `docker build` context if needed in CI)

## 8. Tests

- [x] 8.1 Unit tests for `GitHubReleaseClient` — mock HTTP responses for releases list, ZIP download, error cases
- [x] 8.2 Unit tests for atomic extraction — successful swap, failed extraction leaves old directory intact
- [x] 8.3 Unit tests for version check — matching version skips download, differing version triggers download, missing version.txt always downloads
- [x] 8.4 Update `RuleSetRegistryActorTests` for the new refresh modes (github-release and legacy-url)
- [x] 8.5 Integration test: validate all committed `data/community/rulesets/*.json` files deserialize into `RuleSetFile` without errors
