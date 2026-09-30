## Why

The VitePress docs and README have fallen behind the codebase. Several features shipped since the docs were last updated (v0.2.0) are undocumented: OpenTelemetry observability, the Settings page with log viewer, the Download Detail page, and more. The README has stale config prefixes and dead test project names. The landing page only shows 4 feature tiles despite the product having grown significantly.

## What Changes

- Add **observability page** (DE + EN) covering OpenTelemetry tracing, metrics, OTLP config, and Aspire Dashboard setup
- Extend **web-ui.md** (DE + EN) with Download Detail page and Settings page (including log viewer) sections
- Expand **landing page** (DE + EN) from 4 to 6 feature tiles
- Update **VitePress sidebar/nav** to include observability page
- Fix **README**: stale `MatchHistory` config prefix, dead test project names, missing features in features list, missing config vars

### No new capabilities introduced - this is purely documentation.

## Capabilities

### New Capabilities

_None - documentation-only change._

### Modified Capabilities

_None - no spec-level behavior changes._

## Impact

- `docs/` - modified and new markdown files, config.ts sidebar update
- `README.md` - config table fixes, features list expansion, test project name fixes
- No code changes, no API changes, no dependency changes
