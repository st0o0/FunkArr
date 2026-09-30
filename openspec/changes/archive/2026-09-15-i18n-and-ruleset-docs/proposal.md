## Why

FunkArr's primary audience is in the DACH region (Germany, Austria, Switzerland), yet both the documentation site and the Vue.js frontend are English-only. The ruleset documentation — the most complex user-facing feature — is a 5-bullet placeholder with no field reference, no strategy guide, and no examples. Users currently have to reverse-engineer the JSON schema or read community rulesets to understand how to create their own.

Adding i18n (EN/DE plus AT/CH dialect variants as a fun touch) and comprehensive ruleset documentation addresses both gaps in one change.

## What Changes

- Add `vue-i18n` to the frontend, extract all hardcoded strings from ~30 .vue files into locale JSON files (en, de, de-AT, de-CH)
- Add locale switcher to the frontend nav with localStorage persistence
- Configure VitePress built-in i18n with folder-based locales, default locale DE
- Write comprehensive ruleset creation guide: field reference, all 5 strategies with real examples, filter cookbook, title construction, merge/override behavior, debugger walkthrough
- Translate all existing docs (getting-started, configuration, rulesets) to German
- DE-AT/DE-CH dialect strings for UI chrome only (nav, buttons, headings, empty states, toasts) — collaborative effort, fallback chain de-AT/de-CH → de → en

## Capabilities

### New Capabilities
- `frontend-i18n`: Vue.js frontend internationalization setup — vue-i18n integration, locale JSON files, string extraction, locale switcher, fallback chain, localStorage persistence
- `docs-i18n`: VitePress documentation site internationalization — folder-based locale structure, locale switcher, German translations of all existing content
- `ruleset-guide`: Comprehensive ruleset creation documentation — field reference, strategy guide, filter cookbook, title construction, merge behavior, debugger walkthrough

### Modified Capabilities
- `ruleset-builder-ui`: Locale-aware string rendering in the builder form (labels, placeholders, validation messages, section headings)
- `ruleset-debugger-ui`: Locale-aware string rendering in the debugger panel

## Impact

- **Frontend**: All .vue files updated to use `$t()` calls instead of hardcoded strings; new `src/i18n/` directory with locale JSON files; `vue-i18n` added as dependency
- **Docs**: Restructured from flat files to locale folders (`/en/`, `/de/`); VitePress config updated for i18n; new ruleset guide pages added
- **Dependencies**: `vue-i18n` package added to FunkArr.UI
- **No backend changes**: API error messages stay English (machine-consumed)
