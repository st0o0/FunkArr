## 1. Collapsed Rule Header Metadata

- [ ] 1.1 Update `RuleSetDetail.vue` collapsed header: add filter condition count and title rule count as muted text between strategy badge and priority. Format: "N Filter" and/or "N Titelregel(n)". Show dot separator between counts. Hide counts when zero.

## 2. Rename Scoring History Button

- [ ] 2.1 Update i18n key `detail.scoringHistory` in EN ("Scoring History" — keep as-is) and DE ("Scoring-Verlauf" instead of "Bewertungsverlauf"). Also update DE-AT and DE-CH locale files.
- [ ] 2.2 Update `scoring.historyTitle` i18n key similarly for the Scoring History page heading and breadcrumb.

## 3. Merge Identity and Source Sections

- [ ] 3.1 Update `RuleSetDetail.vue`: merge Identity and Source into one card. Keep the identity grid rows, add source badge + update timestamp as a row at the bottom of the same grid (or as a footer line in the same card). Remove the separate "Quelle" section heading and card.

## 4. Verify

- [ ] 4.1 Run `vue-tsc --noEmit` and verify no errors.
