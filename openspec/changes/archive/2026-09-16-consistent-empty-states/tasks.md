## 1. Scoring History Empty State

- [ ] 1.1 Update `ScoringHistory.vue`: replace plain text `$t('scoring.noHistory')` with `EmptyState` component using a clock icon, the existing i18n title, and a new description key explaining when history appears.
- [ ] 1.2 Add `scoring.noHistoryDescription` i18n key to EN and DE locale files.

## 2. Home Recent Activity Empty State

- [ ] 2.1 Update `Home.vue`: replace the plain `<p>` tags in the recent activity empty state with `EmptyState` component using a download icon, the existing `home.noRecentActivity` title, and `home.noRecentActivityHint` as description.

## 3. RuleSet Detail No-Rules Empty State

- [ ] 3.1 Update `RuleSetDetail.vue`: replace plain text "no rules defined" with `EmptyState` component using a rules/list icon, the existing i18n title, and a new description key.
- [ ] 3.2 Add `detail.noRulesDescription` i18n key to EN and DE locale files.

## 4. Verify

- [ ] 4.1 Run `vue-tsc --noEmit` to verify no TypeScript errors.
- [ ] 4.2 Rebuild container and verify all three empty states render consistently with icons and descriptions.
