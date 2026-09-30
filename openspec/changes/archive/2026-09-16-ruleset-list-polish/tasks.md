## 1. Conditional Source Filter

- [ ] 1.1 Update `RuleSetList.vue`: hide source filter tabs when all rulesets have the same source type. Only show source tabs when there's at least 2 different source types.

## 2. Smart Badge Display

- [ ] 2.1 Update `RuleSetList.vue`: only show media type badge for "movie" rulesets (shows are the default, no badge needed). Only show source badge when multiple source types exist.

## 3. Alias Truncation

- [ ] 3.1 Update `RuleSetList.vue`: truncate alias display to max 2 aliases, show "+N" suffix when more exist.

## 4. Verify

- [ ] 4.1 Run `vue-tsc --noEmit` and verify no errors.
