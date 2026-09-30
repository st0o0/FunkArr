## Context

FunkArr's Vue.js frontend (~30 .vue files) has all user-facing strings hardcoded in English templates. The VitePress documentation site (3 guide pages + 3 ruleset pages) is also English-only with no i18n configuration. The ruleset documentation (`custom.md`) is a 5-bullet placeholder — the most complex user-facing feature has no field reference, no strategy guide, and no examples. The primary audience is DACH (Germany/Austria/Switzerland).

The RuleSet JSON format has substantial complexity: 5 identification strategies, nested filter groups with 6 operators across 6 fields, title construction via composable parts, and a community/local merge system with standalone mode and selective rule disabling. This warrants thorough documentation.

## Goals / Non-Goals

**Goals:**
- Comprehensive EN ruleset documentation that covers every field, strategy, and pattern
- Full DE translation of docs and frontend
- AT/CH dialect strings as fun flavor for UI chrome
- Clean i18n infrastructure that scales with new features

**Non-Goals:**
- Backend/API i18n — error messages stay English (machine-consumed)
- RTL or non-Latin script support
- Automated translation pipeline or CI enforcement of translation completeness
- Full AT/CH translation — only personality strings, rest falls back to DE

## Decisions

### 1. vue-i18n for frontend i18n

**Decision:** Use `vue-i18n` (the official Vue.js i18n library).

**Rationale:** It's the standard for Vue 3 projects. Supports composition API (`useI18n()`), locale fallback chains, lazy loading, and pluralization. No realistic alternatives for Vue.js.

**Locale fallback chain:**
```
de-AT → de → en
de-CH → de → en
de    → en
en    (root)
```

AT/CH locale files only contain strings where dialect adds fun — everything else cascades to `de`.

### 2. Flat JSON locale files with nested keys

**Decision:** Organize locale strings in flat JSON files (`en.json`, `de.json`, `de-AT.json`, `de-CH.json`) under `src/i18n/`, using dot-separated nested keys grouped by view/component.

**Structure:**
```
src/FunkArr.UI/src/i18n/
  index.ts          # vue-i18n setup, locale loading, fallback config
  locales/
    en.json         # Full English strings (source of truth)
    de.json         # Full German translations
    de-AT.json      # Austrian dialect overrides (partial)
    de-CH.json      # Swiss dialect overrides (partial)
```

**Key naming convention:**
```json
{
  "nav": { "home": "Home", "rulesets": "Rulesets", "activity": "Activity" },
  "builder": { "title": "New RuleSet", "identity": "Identity", "save": "Save" },
  "debugger": { "livePreview": "Live Preview", "fullTest": "Full Test" },
  "common": { "loading": "Loading...", "error": "Error", "noResults": "No results" }
}
```

**Rationale:** Nested keys group by feature area, making it easy to find and maintain strings. Flat files (no splitting per view) keep things simple at the current scale (~200 strings). AT/CH files are tiny — only the fun overrides.

### 3. VitePress folder-based i18n with DE as default

**Decision:** Use VitePress built-in i18n with root locale set to DE and EN content under `/en/`.

**Structure:**
```
docs/
  index.md                    # DE (default)
  getting-started.md          # DE
  configuration.md            # DE
  rulesets/
    index.md                  # DE
    catalog.md                # DE
    custom.md                 # DE — expanded ruleset guide
    field-reference.md        # DE — new: complete field reference
    strategies.md             # DE — new: strategy deep-dive
    filters.md                # DE — new: filter cookbook
  en/
    index.md                  # EN
    getting-started.md        # EN
    configuration.md          # EN
    rulesets/
      index.md                # EN
      catalog.md              # EN
      custom.md               # EN
      field-reference.md      # EN
      strategies.md           # EN
      filters.md              # EN
  .vitepress/
    config.ts                 # Updated with i18n config
```

**Rationale:** DE as default because the primary audience is German-speaking. VitePress natively supports this via `locales` in config — no plugins needed. The `/en/` subfolder keeps English content separate without duplicating the VitePress config.

No AT/CH for docs — dialect in technical documentation would hurt readability, and the fun factor belongs in the interactive UI.

### 4. Ruleset documentation structure

**Decision:** Split the current `custom.md` into multiple focused pages.

| Page | Content |
|------|---------|
| `custom.md` | Getting started tutorial — create your first ruleset step-by-step |
| `field-reference.md` | Every field documented: type, required/optional, default, description, example |
| `strategies.md` | All 5 strategies with real community examples (Tatort, Schloss Einstein, heute-show, etc.) |
| `filters.md` | Filter system deep-dive: operators, fields, nested groups, common patterns |

**Rationale:** A single page with everything would be overwhelming. The tutorial (`custom.md`) gives users a quick win, then they can dive into reference material. Real community rulesets as examples ground the docs in actual usage.

### 5. Locale switcher placement

**Decision:** Frontend: dropdown in AppLayout nav bar. Docs: VitePress built-in locale switcher (appears in nav automatically when i18n is configured).

**Locale persistence:** Frontend stores preference in `localStorage` under key `funkarr-locale`. On load, check localStorage → browser `navigator.language` → fallback to `de`.

### 6. String extraction approach

**Decision:** Extract strings view-by-view, replacing hardcoded text with `$t('key')` calls in templates and `t('key')` in `<script setup>`. Placeholders use named interpolation: `$t('builder.validationCount', { count: n })`.

**Not extracting:**
- Technical identifiers displayed as-is (strategy names like `seasonAndEpisodeNumber`, filter field names)
- CSS class names or HTML attributes
- API error messages passed through from backend

## Risks / Trade-offs

**[Every new feature touches translation files]** → Accepted trade-off. At 0.x velocity this is manageable. EN is source of truth — new strings are added to `en.json` first, DE added alongside. AT/CH only when there's something fun to say.

**[Translation drift]** → No CI enforcement planned. The fallback chain means missing DE strings show English, not broken keys. This is acceptable for a solo/small-team project.

**[AT/CH dialect accuracy]** → Collaborative effort with the user. LLM-generated dialect would feel off. The i18n infrastructure is set up, strings are filled in together.

**[Docs content volume]** → The ruleset guide is substantial (field reference + 5 strategies + filter cookbook). Writing it in EN first, then translating to DE, roughly doubles the content work. But the EN version is the most valuable deliverable and stands alone.
