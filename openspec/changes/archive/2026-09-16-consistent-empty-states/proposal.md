## Why

The app already has a well-designed `EmptyState.vue` component (icon + title + description + slot for CTA), but only Activity and RuleSet-List use it. Scoring-History, Home recent-activity, and RuleSet-Detail no-rules all use ad-hoc plain text. This creates inconsistent visual quality and varying helpfulness — some empty states explain what triggers content, others just say "nothing here."

## What Changes

- Update Scoring-History to use `EmptyState` component with icon, title, and description explaining when history appears
- Update Home recent-activity section to use `EmptyState` component instead of plain `<p>` tags
- Update RuleSet-Detail "no rules defined" to use `EmptyState` component
- Add missing i18n description keys where empty states lack explanatory text

## Capabilities

### New Capabilities

_None — using existing `EmptyState` component._

### Modified Capabilities

- `ruleset-ui`: Detail page "no rules" uses EmptyState component
- `empty-states`: Scoring-History and Home use EmptyState component consistently

## Impact

- **Frontend only**: `ScoringHistory.vue`, `Home.vue`, `RuleSetDetail.vue`, i18n locale files
- **No new components**: Existing `EmptyState.vue` already supports all needed props
