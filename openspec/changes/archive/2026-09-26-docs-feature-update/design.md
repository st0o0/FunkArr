## Context

FunkArr's VitePress docs site (DE + EN) and README were last significantly updated at v0.2.0. Since then, several features shipped without documentation updates: OpenTelemetry observability, Settings page with log viewer, Download Detail page, real health checks, network routing/proxy, PUID/PGID support, and download scheduling. The README also has stale config prefixes and dead test project names.

The docs site uses VitePress with DE (root) and EN locales. Each content page exists twice. The sidebar and nav are configured in `docs/.vitepress/config.ts`.

## Goals / Non-Goals

**Goals:**
- Document all shipped features that are currently missing from docs
- Fix stale/incorrect information in README
- Expand landing page feature tiles to better represent current capabilities
- Keep DE and EN docs in sync

**Non-Goals:**
- Restructuring the docs site or changing its theme
- Adding new docs pages beyond observability (web-ui.md expansion covers the UI gaps)
- Translating docs into additional languages
- Adding screenshots or videos

## Decisions

### 1. Observability gets its own page

Observability (OpenTelemetry + Aspire Dashboard) is a distinct operational concern that doesn't fit cleanly into the existing configuration page. A dedicated `observability.md` page allows covering:
- What's traced and metered (custom sources/meters)
- OTLP endpoint configuration
- Aspire Dashboard docker-compose setup
- Env vars (`OTEL_EXPORTER_OTLP_ENDPOINT`)

Alternative: fold into configuration.md. Rejected because configuration.md is already long and observability needs setup context beyond just env vars.

### 2. Download Detail and Settings extend web-ui.md

These are UI pages, so they belong in the existing web-ui.md rather than getting separate pages. Web-ui.md already covers Dashboard, Activity, Setup, Rulesets, and Scoring.

### 3. Landing page expands to 6 tiles

VitePress renders 3-column layout well with 6 tiles. Adding Proxy/Geo-Routing and Observability to the existing 4 (Newznab, SABnzbd, Community Rulesets, Single Container) highlights the operational maturity of the project.

### 4. README config table stays but gets fixed

The README serves users who browse GitHub without visiting the docs site. Keeping the config table (fixed) is better than removing it and pointing to docs only. But we won't expand it to cover every new var - the docs site is the canonical reference.

### 5. File structure

```
docs/
  observability.md           (NEW - DE)
  web-ui.md                  (MODIFIED - add Download Detail + Settings)
  index.md                   (MODIFIED - expand feature tiles)
  en/
    observability.md         (NEW - EN)
    web-ui.md                (MODIFIED)
    index.md                 (MODIFIED)
  .vitepress/
    config.ts                (MODIFIED - add observability to sidebar/nav)
README.md                    (MODIFIED - fix config, features, test names)
```

## Risks / Trade-offs

- **DE/EN drift**: Adding content to both locales in the same PR reduces drift risk, but the EN translation may not be perfect. Acceptable for a docs-only change.
- **README scope creep**: README could grow large if we add every config var. Mitigation: only add the most important missing ones (PUID/PGID, OTEL endpoint) and link to docs for the rest.
