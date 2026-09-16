## Purpose

Vue.js frontend internationalization with vue-i18n supporting EN, DE, DE-AT (Austrian), and DE-CH (Swiss Berndeutsch) locales with fallback chains and localStorage persistence.

## Requirements

### Requirement: vue-i18n integration
The frontend SHALL use `vue-i18n` for all user-facing strings. The i18n instance SHALL be created in `src/i18n/index.ts` and installed as a Vue plugin in `main.ts`. The supported locales SHALL be `en`, `de`, `de-AT`, and `de-CH`.

#### Scenario: i18n plugin installed
- **WHEN** the Vue app initializes
- **THEN** the `vue-i18n` plugin is registered and `$t()` is available in all templates

#### Scenario: Composition API access
- **WHEN** a component uses `<script setup>`
- **THEN** `useI18n()` provides `t()` for programmatic string access

### Requirement: Locale JSON files
The frontend SHALL maintain locale JSON files under `src/i18n/locales/`: `en.json` (full, source of truth), `de.json` (full German translation), `de-AT.json` (partial Austrian dialect overrides), `de-CH.json` (partial Swiss dialect overrides). Keys SHALL use nested dot-separated grouping by view/component area (e.g., `nav.home`, `builder.title`, `common.loading`).

#### Scenario: English source of truth
- **WHEN** a new user-facing string is added
- **THEN** it MUST be added to `en.json` first, then translated in `de.json`

#### Scenario: AT/CH partial coverage
- **WHEN** `de-AT.json` does not contain a key present in `de.json`
- **THEN** the string falls back to the `de.json` value

#### Scenario: Key grouping
- **WHEN** the locale files are organized
- **THEN** keys are grouped by feature area: `nav.*`, `builder.*`, `debugger.*`, `activity.*`, `common.*`, etc.

### Requirement: Locale fallback chain
The i18n configuration SHALL define fallback chains: `de-AT` falls back to `de`, `de-CH` falls back to `de`, `de` falls back to `en`. The fallback locale SHALL be `en`.

#### Scenario: de-AT fallback to de
- **WHEN** locale is `de-AT` and a key exists in `de.json` but not in `de-AT.json`
- **THEN** the German value from `de.json` is displayed

#### Scenario: de fallback to en
- **WHEN** locale is `de` and a key exists in `en.json` but not in `de.json`
- **THEN** the English value from `en.json` is displayed

#### Scenario: Full chain de-CH to de to en
- **WHEN** locale is `de-CH` and a key exists only in `en.json`
- **THEN** the English value is displayed after falling through `de-CH` → `de` → `en`

### Requirement: Locale switcher
The AppLayout nav bar SHALL include a locale switcher dropdown. The switcher SHALL display the current locale name and offer all 4 locales as options: "English", "Deutsch", "Österreichisch", "Schwizerdütsch".

#### Scenario: Switcher in nav
- **WHEN** the AppLayout renders
- **THEN** a locale switcher is visible in the nav bar

#### Scenario: Change locale
- **WHEN** the user selects "Deutsch" from the switcher
- **THEN** the active locale changes to `de` and all visible strings update immediately

#### Scenario: Locale names displayed
- **WHEN** the locale switcher dropdown opens
- **THEN** options are labeled "English", "Deutsch", "Österreichisch", "Schwizerdütsch"

### Requirement: Locale persistence
The selected locale SHALL be persisted in `localStorage` under key `funkarr-locale`. On app load, the locale SHALL be determined by: (1) `localStorage` value if present and valid, (2) browser `navigator.language` mapped to the closest supported locale, (3) fallback to `de`.

#### Scenario: Persist selection
- **WHEN** the user selects "English" from the locale switcher
- **THEN** `localStorage.setItem('funkarr-locale', 'en')` is called

#### Scenario: Restore on reload
- **WHEN** the app loads and `localStorage` contains `funkarr-locale: "de-AT"`
- **THEN** the active locale is set to `de-AT`

#### Scenario: Browser language detection
- **WHEN** no localStorage value exists and `navigator.language` is `de-CH`
- **THEN** the active locale is set to `de-CH`

#### Scenario: Fallback to de
- **WHEN** no localStorage value exists and `navigator.language` is `fr-FR`
- **THEN** the active locale is set to `de`

### Requirement: String extraction from templates
All hardcoded user-facing strings in `.vue` templates SHALL be replaced with `$t('key')` calls. This includes: labels, headings, button text, placeholder text, empty state messages, toast messages, validation messages, and aria labels. Technical identifiers (strategy names, filter field names, API paths) SHALL NOT be extracted.

#### Scenario: Label extraction
- **WHEN** a template contains `<label>Topic</label>`
- **THEN** it SHALL be replaced with `<label>{{ $t('builder.topic') }}</label>`

#### Scenario: Placeholder extraction
- **WHEN** an input has `placeholder="Search..."`
- **THEN** it SHALL be replaced with `:placeholder="$t('common.search')"`

#### Scenario: Interpolation
- **WHEN** a template contains `{{ count }} validation errors`
- **THEN** it SHALL use `$t('builder.validationCount', { count })` with the locale string `"{count} validation errors"`

#### Scenario: Technical strings not extracted
- **WHEN** a template displays strategy name `seasonAndEpisodeNumber`
- **THEN** it SHALL remain as a literal string, not a translation key

### Requirement: HTML lang attribute
The `index.html` SHALL set the `lang` attribute dynamically based on the active locale. The initial value SHALL be `de`.

#### Scenario: Lang attribute matches locale
- **WHEN** the active locale is `de-AT`
- **THEN** `<html lang="de-AT">` is set on the document element
