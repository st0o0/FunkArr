## 1. Release-please config alignment

- [x] 1.1 Update `release-please-config.json`: add `$schema`, `include-component-in-tag: false`, `changelog-sections` (matching Njord), switch `extra-files` from `xml/xpath` to `generic`

## 2. Commitlint

- [x] 2.1 Create `commitlint.config.mjs` (copy from Njord, same type-enum and relaxed rules)
- [x] 2.2 Add `commitlint` job to `.github/workflows/ci.yml` (using `wagoid/commitlint-github-action@v6`, fetch-depth: 0)

## 3. Hadolint

- [x] 3.1 Create `.hadolint.yaml` with `failure-threshold: warning`
- [x] 3.2 Update CI lint job to reference `config: .hadolint.yaml`

## 4. Dependabot

- [x] 4.1 Create `.github/dependabot.yml` with nuget (groups: akka, testing, servus), github-actions, and docker ecosystems — all weekly, `deps` prefix

## 5. Security workflow

- [x] 5.1 Create `.github/workflows/security.yml` with Trivy scan (SARIF, non-blocking) and NuGet audit jobs
- [x] 5.2 Create `.trivyignore` (empty, with header comment explaining its purpose)

## 6. Dev-build workflow

- [x] 6.1 Create `.github/workflows/dev-build.yml` (label-gated, `dev` environment, 2 RIDs: amd64 + arm64, PR comment with pull command)

## 7. CLAUDE.md

- [x] 7.1 Create `CLAUDE.md` at repo root with FunkArr-specific project docs, conventions, and skill routing

## 8. Verification

- [x] 8.1 Validate all YAML files are syntactically correct
