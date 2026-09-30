## 1. Frontend i18n Infrastructure

- [x] 1.1 Add `vue-i18n` dependency via pnpm in `src/FunkArr.UI`
- [x] 1.2 Create `src/i18n/index.ts` with vue-i18n instance, locale fallback chains (de-AT→de→en, de-CH→de→en), and localStorage persistence under key `funkarr-locale`
- [x] 1.3 Create `src/i18n/locales/en.json` with all English strings extracted from .vue files, grouped by feature area (nav, builder, debugger, activity, common, etc.)
- [x] 1.4 Install vue-i18n plugin in `main.ts`

## 2. Frontend String Extraction

- [x] 2.1 Extract strings from `AppLayout.vue` (nav items, sidebar labels) and replace with `$t()` calls
- [x] 2.2 Extract strings from `Home.vue`, `Activity.vue`, `Setup.vue` (headings, labels, empty states)
- [x] 2.3 Extract strings from `RuleSetList.vue`, `RuleSetDetail.vue` (headings, badges, buttons, table headers)
- [x] 2.4 Extract strings from `RuleSetBuilder.vue` (section headings, labels, placeholders, button text, validation messages)
- [x] 2.5 Extract strings from `DebuggerPanel.vue`, `LiveMatchPreview.vue` (mode indicators, badges, status text, disclaimers)
- [x] 2.6 Extract strings from remaining components: `QueueCard.vue`, `QueueGroupCard.vue`, `HealthWidget.vue`, `ActiveDownloads.vue`, `FilterGroupTraceView.vue`, `EmptyState.vue`, `ToastContainer.vue`, `ScoringHistory.vue`, `ScoringDetail.vue`
- [x] 2.7 Update `index.html` lang attribute to `de` and add dynamic lang update in i18n setup

## 3. Frontend Locale Switcher

- [x] 3.1 Add locale switcher dropdown to `AppLayout.vue` nav bar with options: English, Deutsch, Österreichisch, Schwizerdütsch
- [x] 3.2 Wire switcher to change active locale, persist to localStorage, and update document `lang` attribute

## 4. German Frontend Translations

- [x] 4.1 Create `src/i18n/locales/de.json` with full German translations of all strings from `en.json`
- [x] 4.2 Create `src/i18n/locales/de-AT.json` with placeholder structure (empty object initially — dialect strings filled in collaboratively)
- [x] 4.3 Create `src/i18n/locales/de-CH.json` with placeholder structure (empty object initially — dialect strings filled in collaboratively)

## 5. Ruleset Documentation (EN)

- [x] 5.1 Rewrite `docs/rulesets/custom.md` as a step-by-step tutorial: create a ruleset from scratch using the builder UI, covering identity, rules, strategy selection, debugger testing, and saving
- [x] 5.2 Create `docs/rulesets/field-reference.md` with every field documented: name, type, required/optional, default, description, example — organized by section (root, media, rule, filter, title rule)
- [x] 5.3 Create `docs/rulesets/strategies.md` with all 5 strategies explained using real community examples (Tatort for titleIncludes, Schloss Einstein for absoluteEpisode, heute-show for airdate, etc.) plus a decision guide
- [x] 5.4 Create `docs/rulesets/filters.md` with operator reference, field reference, nested group logic, and common filter patterns (duration, channel, regex)
- [x] 5.5 Update `docs/.vitepress/config.ts` sidebar to include new ruleset guide pages

## 6. VitePress i18n Setup

- [x] 6.1 Update `docs/.vitepress/config.ts` with i18n locale config: root=de, en subfolder, localized nav/sidebar labels
- [x] 6.2 Move current English docs content to `docs/en/` subfolder (index.md, getting-started.md, configuration.md, rulesets/)

## 7. German Documentation Content

- [x] 7.1 Translate `docs/index.md` to German (landing page)
- [x] 7.2 Translate `docs/getting-started.md` to German
- [x] 7.3 Translate `docs/configuration.md` to German
- [x] 7.4 Translate `docs/rulesets/index.md`, `catalog.md` to German
- [x] 7.5 Translate `docs/rulesets/custom.md` (tutorial) to German
- [x] 7.6 Translate `docs/rulesets/field-reference.md` to German
- [x] 7.7 Translate `docs/rulesets/strategies.md` to German
- [x] 7.8 Translate `docs/rulesets/filters.md` to German

## 8. AT/CH Dialect Strings (Collaborative)

- [x] 8.1 Together with user: fill in `de-AT.json` with Austrian dialect strings for nav items, headings, buttons, empty states, toasts
- [x] 8.2 Together with user: fill in `de-CH.json` with Swiss dialect strings for nav items, headings, buttons, empty states, toasts

## 9. Verification

- [x] 9.1 Build frontend (`vue-tsc -b && vite build`) and verify no TypeScript errors from i18n changes
- [x] 9.2 Build docs site (`vitepress build`) and verify both locales render correctly with all pages
- [x] 9.3 Run `dotnet format src/FunkArr.slnx --verify-no-changes` (no .cs changes expected, but verify nothing broke)
