## Why

The docs comprehensively document the ruleset JSON format (field reference, strategies, filters) but users must mentally translate that reference into valid JSON by hand. An interactive builder embedded in the docs site closes that gap — users visually construct a ruleset and get valid, downloadable JSON without needing a running FunkArr instance. This also lets community contributors build and validate rulesets before submitting PRs.

Additionally, the docs site references `/logo.svg` for its favicon and logo but no such file exists in `docs/public/`, so the site currently shows no favicon.

## What Changes

- New VitePress page at `/rulesets/builder` (DE) and `/en/rulesets/builder` (EN) with sidebar entries in both locales
- Interactive Vue component suite for building rulesets:
  - Identity form (topic, aliases, media fields, confidence)
  - Rule cards with strategy-dependent field visibility
  - Filter builder (all/any/not sections with conditions)
  - Title rule list builder (static/regex parts)
  - Live JSON preview with syntax highlighting
  - Client-side validation (schema structure, regex syntax, warnings for missing external IDs)
  - JSON import (paste existing JSON to edit)
  - JSON export (copy to clipboard, download as .json file)
- Responsive layout: side-by-side on desktop, stacked on mobile
- Copy `src/FunkArr.UI/public/favicon.svg` to `docs/public/logo.svg` so the docs site uses the same favicon as the main UI

## Capabilities

### New Capabilities
- `docs-ruleset-builder`: Interactive Vue component in VitePress docs for visually constructing ruleset JSON with validation, import, and export

### Modified Capabilities
- `docs-i18n`: New builder page added to both DE and EN locales with sidebar entries

## Impact

- `docs/.vitepress/config.ts` — new sidebar entries for builder page in both locales
- `docs/.vitepress/theme/` — component registration and new Vue components
- `docs/rulesets/builder.md` and `docs/en/rulesets/builder.md` — new pages embedding the builder component
- `docs/public/logo.svg` — new file (copy of UI favicon)
- No backend changes, no API changes, no build changes to the .NET solution
