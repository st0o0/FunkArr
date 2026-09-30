## 1. Add Ruleset Stat Card

- [ ] 1.1 Update `Home.vue`: change stats grid from `grid-cols-3` to `grid-cols-4`. Add a 4th card linking to `/rulesets` showing ruleset count and community version.
- [ ] 1.2 Fetch ruleset list in `onMounted` using `listRuleSets()` from rulesets API. Store count and community version.
- [ ] 1.3 Add i18n keys for "Rulesets" stat label in EN and DE.

## 2. Improve Storage Bar

- [ ] 2.1 Increase storage progress bar height from `h-1` to `h-1.5` for better visibility.

## 3. Verify

- [ ] 3.1 Run `vue-tsc --noEmit` and verify no errors.
