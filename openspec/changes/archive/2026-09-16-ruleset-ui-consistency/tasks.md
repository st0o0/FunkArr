## 1. Shared Rule Vocabulary Utility

- [x] 1.1 Create `src/FunkArr.UI/src/utils/ruleVocabulary.ts` with functions: `opSymbol(op)` returning compact symbols (`=`, `∋`, `∌`, `>`, `<`, `≈`), `opLabel(op, t)` returning localized operator names, `groupLabel(group, t)` returning localized group labels, `fieldLabel(field, t)` returning localized field names, `titlePartLabel(type, t)` returning localized title part type names. All with fallback to raw string for unknown values.

## 2. i18n Keys

- [x] 2.1 Add `rule` namespace to `en.json` with sub-objects: `op` (eq, contains, notContains, greaterThan, lessThan, regex), `group` (all, any, not), `field` (title, topic, channel, description, duration, timestamp), `titlePart` (static, regex).
- [x] 2.2 Add matching `rule` namespace to `de.json` with German translations.

## 3. Filter Condition Display Component

- [x] 3.1 Update `FilterConditionDisplay.vue`: use `opSymbol()`, `groupLabel()`, and `fieldLabel()` from shared vocabulary. Render each non-empty group as a visually distinct container with a localized header and condition rows showing `fieldLabel opSymbol value`.

## 4. Title Rule Display Component

- [x] 4.1 Create `TitleRuleDisplay.vue` component: render `TitleRuleOutput[]` as ordered list. Each part shows type as a small badge (`static`/`regex` via `titlePartLabel()`). Static parts show quoted value. Regex parts show `fieldLabel() → /pattern/` with optional capture group.

## 5. Detail-View Collapsible Rules

- [x] 5.1 Update `RuleSetDetail.vue`: wrap each rule in a collapsible card with click-to-toggle header (ID in monospace + strategy badge + prio). First rule expanded by default, rest collapsed. Add expand/collapse chevron icon.
- [x] 5.2 Update `RuleSetDetail.vue` expanded state: use `FilterConditionDisplay` and `TitleRuleDisplay` components for structured rendering. Keep season/episode regex and capture group as labeled monospace rows.

## 6. Builder Dropdown Localization

- [x] 6.1 Update `RuleSetBuilder.vue` filter section: group headers use `groupLabel()`, op dropdown options use `opLabel()`, field dropdown options use `fieldLabel()`. Preserve enum values as option values.
- [x] 6.2 Update `RuleSetBuilder.vue` title rules section: type picker options use `titlePartLabel()`, regex field dropdown uses `fieldLabel()`. Preserve enum values as option values.

## 7. Trace View Updates

- [x] 7.1 Update `FilterGroupTraceView.vue`: use `opSymbol()` for operator display and `groupLabel()` for group header instead of raw strings. Preserve pass/fail/skip coloring.

## 8. Verify

- [x] 8.1 Run `vue-tsc --noEmit` to verify no TypeScript errors.
- [x] 8.2 Rebuild container, verify Detail-View shows collapsible rules with localized filter ops/groups/fields and title rule badges.
- [x] 8.3 Verify Builder dropdowns show localized labels in DE locale.
- [x] 8.4 Verify Debugger/LiveMatchPreview trace views show op symbols and localized group headers.
