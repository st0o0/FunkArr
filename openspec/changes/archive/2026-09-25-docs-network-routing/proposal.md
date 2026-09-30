## Why

The network routing and proxy feature (change `network-routing-and-proxy`) has been implemented but is undocumented. Users who want to access geo-restricted content from ORF (Austria) or SRF (Switzerland) need to know how to configure routes, set up proxy infrastructure, and understand how the routing flows through the download pipeline.

## What Changes

- **`docs/configuration.md` (DE):** New section "Netzwerk-Routen" between Downloads and Regelwerke with variable table, pattern matching explanation, Docker Compose example with tinyproxy + WireGuard, and no-auto-fallback note. Update FFmpeg section to mention proxy routing.
- **`docs/en/configuration.md` (EN):** Same content, English version. New section "Network Routes" between Downloads and Rulesets.
- **`docs/how-it-works.md` (DE):** Add paragraph in "FFmpeg-Remux" section explaining route-aware download pipeline.
- **`docs/en/how-it-works.md` (EN):** Same paragraph, English version.

## Capabilities

### New Capabilities

_(none - documentation only, no new code capabilities)_

### Modified Capabilities

_(none - documentation only)_

## Impact

- 4 existing VitePress markdown files modified (2 DE, 2 EN)
- No new pages, no sidebar config changes
- No code changes
