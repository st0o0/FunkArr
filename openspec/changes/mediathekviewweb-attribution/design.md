## Context

MediathekViewWeb is credited only as a health check label and in technical prose. There is no visible attribution or link anywhere prominent in the docs or UI. This change adds three attribution points: docs landing page feature tile, how-it-works callout, and UI sidebar footer link.

## Goals / Non-Goals

**Goals:**
- Give MediathekViewWeb visible credit with a link to `https://mediathekviewweb.de/#everywhere=true`
- Add attribution in docs (landing page tile + how-it-works callout) for both DE and EN locales
- Add a persistent "Powered by" link in the UI sidebar footer visible on every page
- Support collapsed sidebar state (icon-only or hidden)

**Non-Goals:**
- No backend API changes
- No new "About" page or credits section
- No changes to the health check system
- No MediathekViewWeb logo/image assets

## Decisions

### 1. Docs landing page: feature tile (not hero tagline)

Add a 7th feature tile rather than modifying the hero tagline. The tagline already has the right density. A dedicated tile gives MVW its own space with a proper link. VitePress home layout `features` array supports markdown in `details`, which allows inline links.

### 2. How-it-works: VitePress `:::tip` container

Use VitePress's built-in custom container `:::tip` at the start of the "MediathekViewWeb-Abfrage" section (step 3). This visually distinguishes the attribution from the technical content and draws attention.

### 3. UI sidebar: link below version number

Place the link in the existing sidebar footer area in `AppLayout.vue`, below the version number `div`. Use the same `text-[11px] text-text-muted` styling as the version for consistency. The link opens in a new tab (`target="_blank" rel="noopener"`). When the sidebar is collapsed, hide the text (same pattern as version number: `v-if="!collapsed"`).

### 4. i18n key structure

Add keys under a new `attribution` namespace in each locale file:
- `attribution.poweredBy` - "Powered by {link}" (EN) / "Betrieben mit {link}" (DE)

Use the component interpolation approach: the link text "MediathekViewWeb" is hardcoded (it's a proper noun, not translatable), and the surrounding text comes from i18n. Since VitePress docs are static markdown, no i18n is needed there - each locale has its own markdown file.

## Risks / Trade-offs

- [Feature tile count goes from 6 to 7] - VitePress renders 3-column grids for features, so 7 tiles creates an uneven last row (one tile alone). This is acceptable and common in VitePress sites. Alternatively could restructure to 8 tiles in the future.
- [External link in sidebar] - Users clicking it leave the app. Mitigated by `target="_blank"`.
