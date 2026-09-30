## Why

The RuleSet list has two filter tab rows (type + source) that are visually identical and redundant when all rulesets are community-only. Badges like "show" (59/69) and "community" (69/69) carry no information when they're the overwhelming majority.

## What Changes

- Hide source filter tabs when all rulesets share the same source (all community = no filter needed)
- Hide media type badge when it matches the majority type (only show "movie" badge since shows are the default)
- Hide source badge when all rulesets are the same source type
- Tighten alias display — truncate long alias lists

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `ruleset-ui`: List page conditional badge and filter visibility

## Impact
- **Frontend only**: `RuleSetList.vue`
