## Why

Dashboard shows system health, download count, and storage, but nothing about rulesets — the core of what FunkArr does. On a fresh setup the page is mostly empty.

## What Changes

- Add a 4th stat card linking to `/rulesets` showing ruleset count and community version
- Widen stats grid from 3 to 4 columns
- Improve storage bar visibility (thicker)

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `ruleset-ui`: Dashboard shows ruleset count stat

## Impact
- **Frontend only**: `Home.vue`, `rulesets.ts` API client (use existing list endpoint)
