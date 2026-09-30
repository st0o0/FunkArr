## Context

FunkArr and Njord share the same author, tech stack (Akka.NET, Servus, xUnit v3, Docker multi-arch), and release pipeline shape (release-please → dotnet publish → Docker → GHCR → cosign). But FunkArr is missing several infrastructure pieces that Njord has battle-tested. This change ports them over, adapted for FunkArr's specifics (2 RIDs instead of 3, FFmpeg dependency, different config namespace).

## Goals / Non-Goals

**Goals:**
- Identical CI quality gates as Njord (commitlint, hadolint, dotnet format, security scan)
- Dependabot with grouped PRs to reduce noise
- Dev-build workflow for testing Docker images from PRs
- Release-please config that produces clean changelogs
- CLAUDE.md that captures FunkArr-specific conventions

**Non-Goals:**
- Adding armv7 support (FunkArr only targets amd64 + arm64)
- Documentation site / VitePress (Njord-specific)
- Changing any application code

## Decisions

### 1. Copy Njord's commitlint config verbatim

Same type-enum, same relaxed rules (no subject-case enforcement, no body/footer line length). Same dependabot ignore for `dependabot[bot]` commits.

**Why:** Identical commit discipline across projects. The relaxed ruleset was deliberately chosen to avoid rejecting valid commits over stylistic preferences.

### 2. Security workflow: Trivy + NuGet audit, non-blocking

Trivy scans the Docker image and uploads SARIF to the Security tab (exit-code 0 = non-blocking). NuGet audit lists vulnerable packages. Runs on PRs touching Dockerfile/packages/csproj, weekly schedule, and manual dispatch.

**Why:** Matches Njord. Non-blocking because base image CVEs are often unfixable upstream — `.trivyignore` tracks known exceptions.

### 3. Dev-build with `dev` environment gate

Label-gated (`dev-build` label) + environment approval (`dev` environment). Builds multi-arch images tagged `pr-<N>` and `dev-<shortsha>`, comments the pull command on the PR.

**Why:** Matches Njord. Environment gate prevents unauthorized image pushes from forks. Only 2 platforms (amd64, arm64) unlike Njord's 3.

### 4. release-please: switch from xml/xpath to generic extra-files

The `x-release-please-version` comment is already in `Directory.Build.props`. Switching to `generic` type makes release-please use the comment marker directly, consistent with Njord.

**Why:** The `xml/xpath` approach works but ignores the comment marker. If the XML structure changes, xpath breaks silently. The comment marker is self-documenting and robust.

### 5. CLAUDE.md: FunkArr-specific, not a copy of Njord

Adapted for FunkArr's architecture (Mediathek search, download pipeline, SABnzbd/Newznab APIs, FFmpeg muxing). Includes same structural sections as Njord: Project, Build & test, Conventions, Workflow, Skill routing.

**Why:** CLAUDE.md must reflect the actual project. Copying Njord's would be misleading.

## Risks / Trade-offs

- **[commitlint requires npm]** → The CI job uses `wagoid/commitlint-github-action` which handles npm internally. No local npm needed.
- **[Trivy SARIF requires GitHub Advanced Security]** → Available free on public repos. If the repo is private, the SARIF upload step will fail silently — acceptable.
- **[dev-build environment must be created manually]** → Requires one-time setup in GitHub Settings → Environments → create `dev` with required reviewers.
