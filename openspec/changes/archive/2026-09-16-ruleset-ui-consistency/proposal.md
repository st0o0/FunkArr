## Why

The RuleSet API now returns structured typed data (filter ops, fields, groups, title rules as enums), but all frontend views render these as raw enum names — `greaterThan`, `contains`, `eq`, `all`/`any`/`not`, `static`/`regex`. Each view handles formatting independently with no shared vocabulary, resulting in inconsistent, unlocalizable, user-unfriendly displays. The Detail-View also shows all rules flat without collapse, making complex rulesets hard to scan.

## What Changes

- Create shared `ruleVocabulary.ts` utility with localized labels and symbols for filter ops (`=`, `>`, `<`, `∋`, `∌`, `≈`), filter groups ("alle erfüllt"/"mind. eins"/"keines"), fields ("Titel"/"Thema"/"Dauer"...), and title part types
- Add `rule` i18n namespace with DE+EN keys for all ops, groups, fields, and title part types
- Update `FilterConditionDisplay.vue` with visual grouping containers, op symbols, and localized labels
- Create `TitleRuleDisplay.vue` component with type badges and structured field→pattern layout
- Update `FilterGroupTraceView.vue` to use shared op symbols and group labels
- Add collapsible rule cards to `RuleSetDetail.vue` (collapsed: ID + strategy badge + prio, expanded: full details)
- Update `RuleSetBuilder.vue` dropdowns (filter ops, fields, groups, title rule types) to use shared localized labels

## Capabilities

### New Capabilities

- `rule-vocabulary`: Shared utilities and i18n keys for consistent rendering of filter operators, fields, groups, and title part types across all views

### Modified Capabilities

- `ruleset-ui`: Detail-View gets collapsible rule cards and uses shared display components for filters and title rules
- `ruleset-builder-ui`: Builder dropdowns use shared vocabulary for localized op/field/group labels
- `ruleset-debugger-ui`: FilterGroupTraceView uses shared vocabulary for consistent op/group rendering

## Impact

- **Frontend only**: `utils/`, `components/`, `views/`, `i18n/locales/` — no backend changes
- **Affected views**: RuleSetDetail, RuleSetBuilder, DebuggerPanel, LiveMatchPreview, ScoringDetail
- **Affected components**: FilterConditionDisplay (update), FilterGroupTraceView (update), TitleRuleDisplay (new)
- **No API changes**: Frontend-only rendering improvements
