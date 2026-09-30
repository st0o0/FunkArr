## 1. Favicon and Page Scaffolding

- [x] 1.1 Copy `src/FunkArr.UI/public/favicon.svg` to `docs/public/logo.svg`
- [x] 1.2 Add builder sidebar entries to both locales in `docs/.vitepress/config.ts`
- [x] 1.3 Create `docs/rulesets/builder.md` (German) with page heading and `<RulesetBuilder />` component tag
- [x] 1.4 Create `docs/en/rulesets/builder.md` (English) with page heading and `<RulesetBuilder />` component tag

## 2. Theme Setup

- [x] 2.1 Register global Vue components in `docs/.vitepress/theme/index.ts` (import and register all builder components)
- [x] 2.2 Add builder-specific CSS to `docs/.vitepress/theme/custom.css` (form styling, layout grid, validation colors, responsive breakpoints)

## 3. Core State and Orchestrator

- [x] 3.1 Create `RulesetBuilder.vue` — reactive state object matching JSON schema, computed JSON output with empty-field cleanup, validation logic with debounce, import/export handlers

## 4. Identity Form

- [x] 4.1 Create `IdentityForm.vue` — topic, aliases (add/remove), media object (name, type dropdown, tvdbId, imdbId, tmdbId), confidence (0.0–1.0), auto-fill media name from topic

## 5. Rule Cards

- [x] 5.1 Create `RuleCard.vue` — rule id, priority, strategy dropdown, optional confidence override, add/remove rule, collapsible card layout
- [x] 5.2 Create `StrategyFields.vue` — conditional rendering: seasonRegex/episodeRegex/captureGroup for regex strategies, title rules builder slot for title strategies

## 6. Filter Builder

- [x] 6.1 Create `FilterBuilder.vue` — three collapsible sections (ALL expanded, ANY/NOT collapsed), condition rows (field dropdown, operator dropdown, value input), add/remove conditions, omit empty sections from output

## 7. Title Rules

- [x] 7.1 Create `TitleRuleList.vue` — ordered list of parts, add static (value input) or regex (field dropdown, pattern input, optional capture group) parts, remove parts, regex syntax validation

## 8. JSON Preview and Actions

- [x] 8.1 Create `JsonPreview.vue` — syntax-highlighted JSON display, reactive updates, copy-to-clipboard button with confirmation, download button (kebab-case filename from topic)
- [x] 8.2 Create `ImportModal.vue` — modal with textarea, paste JSON, validate on import, populate builder state, error display for invalid JSON

## 9. Validation

- [x] 9.1 Implement validation rules in `RulesetBuilder.vue` — errors: missing required fields (topic, media.name, strategy), invalid regex syntax, duplicate rule IDs, invalid rule ID format; warnings: no external ID, zero rules; inline error display per field, summary area for warnings

## 10. Responsive Layout

- [x] 10.1 Implement responsive CSS — side-by-side at >960px (form left, preview right), stacked at <=960px (form above, preview below), consistent with VitePress breakpoints

## 11. Verification

- [x] 11.1 Start docs dev server (`pnpm dev` in docs/), verify builder page loads in both locales, test full workflow: fill identity, add rule with each strategy type, add filters, import/export JSON, verify favicon and logo display
