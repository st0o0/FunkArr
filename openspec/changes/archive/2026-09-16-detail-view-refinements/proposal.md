## Why

The RuleSet detail page has small UX gaps: collapsed rule headers show only ID + strategy + prio without hinting at what's inside, the "Bewertungsverlauf" button label is unclear, and the Identity + Source sections use separate full-width cards for relatively little content.

## What Changes

- Add small metadata counts to collapsed rule headers (filter count, title rule count)
- Rename "Bewertungsverlauf" → "Scoring-Verlauf" across all views and i18n keys
- Merge Identity and Source into one combined section card
- Remove redundant display of topic (shown both as page title and in Identity grid)

## Capabilities

### New Capabilities
_None._

### Modified Capabilities
- `ruleset-ui`: Detail page layout and collapsed rule header improvements

## Impact
- **Frontend only**: `RuleSetDetail.vue`, i18n locale files (EN, DE, DE-AT, DE-CH)
