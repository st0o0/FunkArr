## Why

FunkArr was bootstrapped from Njord's patterns but is missing several CI/CD components, security workflows, and configuration conventions that Njord has matured over time. Aligning now — before the first release — avoids drift and ensures the same quality gates from day one.

## What Changes

### Bucket A — Config alignment (quick wins)
- Fix `release-please-config.json`: add `$schema`, `include-component-in-tag: false`, `changelog-sections`, switch `extra-files` from `xml/xpath` to `generic` (consistent with the `x-release-please-version` comment marker already in `Directory.Build.props`)
- Add `.hadolint.yaml` (failure-threshold: warning)
- Add `CLAUDE.md` with project-specific conventions

### Bucket B — CI parity
- Add `.github/dependabot.yml` with grouped updates (nuget: akka/testing/servus, github-actions, docker)
- Add commitlint (CI job in `ci.yml` + `commitlint.config.mjs`)
- Add `.github/workflows/security.yml` (Trivy container scan + NuGet vulnerability audit)
- Add `.trivyignore` (empty or with known base-image CVEs)

### Bucket C — Dev workflow
- Add `.github/workflows/dev-build.yml` (label-gated PR docker images with `pr-<N>` and `dev-<sha>` tags, environment-approved)

## Capabilities

### New Capabilities
- `ci-infrastructure`: CI/CD pipeline configuration including commitlint, dependabot, security scanning, dev builds, and release-please alignment with Njord conventions

### Modified Capabilities

## Impact

- `.github/workflows/ci.yml`: add commitlint job
- `.github/workflows/security.yml`: new workflow
- `.github/workflows/dev-build.yml`: new workflow
- `.github/dependabot.yml`: new file
- `release-please-config.json`: config changes (non-breaking, affects future changelogs)
- `commitlint.config.mjs`: new file
- `.hadolint.yaml`: new file
- `.trivyignore`: new file
- `CLAUDE.md`: new file
- No code changes, no API changes
