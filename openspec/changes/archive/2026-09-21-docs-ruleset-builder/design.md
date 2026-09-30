## Context

The FunkArr docs site is built with VitePress (Vue-based SSG) and already contains comprehensive ruleset documentation: field reference, strategies, filters, and a custom rulesets tutorial. The tutorial describes the in-app builder UI but offers no way to construct JSON without running FunkArr. Users wanting to contribute community rulesets or prepare configurations must write JSON by hand using the reference pages.

VitePress natively supports Vue components inside Markdown pages — no extra build tooling needed. The docs already use a custom theme (`docs/.vitepress/theme/`) with brand colors (Amber `#e67e22`).

The docs site also has a broken favicon: `config.ts` references `/logo.svg` but `docs/public/` contains only a `CNAME` file. The UI favicon exists at `src/FunkArr.UI/public/favicon.svg`.

## Goals / Non-Goals

**Goals:**
- Interactive builder page where users construct valid ruleset JSON visually
- Works entirely client-side — no FunkArr instance required
- Import existing JSON to edit, export via copy/download
- Client-side validation with inline feedback
- Responsive layout (desktop: side-by-side, mobile: stacked)
- Consistent with VitePress theme and FunkArr brand
- Fix the missing docs favicon

**Non-Goals:**
- Live testing against MediathekViewWeb API (requires server)
- Pipeline debugger / trace visualization
- Saving to or loading from a FunkArr instance
- Nested filter groups in the first version (flat all/any/not only)

## Decisions

### D1: Vue SFC components in VitePress theme directory

**Decision:** Build the builder as Vue Single File Components registered in the VitePress theme.

**Rationale:** VitePress is built on Vue and supports custom components natively. No additional bundler config, no separate build step, no framework mismatch. Components live in `docs/.vitepress/theme/components/` and are registered globally in `theme/index.ts`.

**Alternative considered:** Standalone web app (React/Svelte) embedded via iframe. Rejected — adds build complexity, breaks theme integration, duplicates styling.

### D2: Reactive state object mirroring JSON schema

**Decision:** The builder state is a single Vue `reactive()` object whose shape matches the ruleset JSON output exactly. The JSON preview is just `JSON.stringify(state)` with cleanup (removing empty/null fields).

**Rationale:** No mapping layer between form state and output. What you see in the preview is always what you get. Changes propagate instantly via Vue reactivity.

**Alternative considered:** Separate form model mapped to JSON on export. Rejected — introduces divergence risk and adds complexity for no benefit.

### D3: Strategy-dependent field visibility

**Decision:** The `RuleCard` component shows/hides fields based on the selected strategy:
- `seasonAndEpisodeNumber`: shows `seasonRegex`, `episodeRegex`, `captureGroup`
- `byAbsoluteEpisodeNumber`: shows `episodeRegex`, `captureGroup`
- `itemTitleExact`, `itemTitleIncludes`, `itemTitleEqualsAirdate`: shows `titleRules` builder

**Rationale:** Matches the actual schema constraints — regex fields are irrelevant for title strategies and vice versa. Reduces confusion for users who don't need to know the full schema.

### D4: Filter builder with flat sections initially

**Decision:** The filter builder shows three collapsible sections (ALL, ANY, NOT). Each section contains a flat list of conditions (field + operator + value). The `all` section is expanded by default; `any` and `not` are collapsed.

Nested filter groups (e.g., an `any` inside an `all`) are deferred to a later iteration. The existing docs note that "most rulesets only need flat conditions."

**Rationale:** Covers 95%+ of real-world rulesets. Nested groups add significant UX complexity (recursive component rendering, indentation, drag-and-drop reordering) for a rarely-used feature.

### D5: Client-side validation

**Decision:** Validate on every state change with debounce (300ms). Three validation levels:
- **Error** (red): Missing required fields (topic, media.name, strategy), invalid regex syntax, duplicate rule IDs
- **Warning** (amber): No external ID set (tvdbId/imdbId/tmdbId), zero rules defined
- **Info** (gray): Contextual hints (e.g., "Set Series Type to Daily in Sonarr for airdate strategies")

Regex patterns are tested via `new RegExp()` in the browser.

### D6: JSON import via textarea modal

**Decision:** An "Import JSON" button opens a modal with a textarea. Paste JSON, click Import, the builder populates from it. Invalid JSON shows an error inline in the modal.

**Rationale:** Simple, no file upload needed. Users typically copy JSON from a GitHub PR or a file editor.

### D7: Favicon — copy the SVG file

**Decision:** Copy `src/FunkArr.UI/public/favicon.svg` to `docs/public/logo.svg`. This is the path `config.ts` already references for both `head` favicon link and `themeConfig.logo`.

**Alternative considered:** Symlink. Rejected — doesn't work reliably across platforms and in GitHub Pages deployment.

### D8: Component structure

```
docs/.vitepress/theme/
  index.ts                    — registers components globally
  custom.css                  — existing brand colors (keep as-is)
  components/
    RulesetBuilder.vue        — orchestrator: state, import/export, validation
    IdentityForm.vue          — topic, aliases[], media object, confidence
    RuleCard.vue              — one rule: id, priority, strategy, filters, fields
    StrategyFields.vue        — conditional fields based on strategy
    FilterBuilder.vue         — all/any/not sections with condition rows
    TitleRuleList.vue          — ordered static/regex title parts
    JsonPreview.vue           — syntax-highlighted JSON, copy, download
    ImportModal.vue           — paste JSON to populate builder
```

Each markdown page (`builder.md`) simply contains `<RulesetBuilder />`.

## Risks / Trade-offs

- **[Regex validation is browser-only]** → JavaScript regex differs slightly from .NET regex (e.g., lookbehinds). A regex that validates in the browser might behave differently in FunkArr. Mitigation: document this limitation with a note on the builder page.
- **[Component bundle size]** → Vue components add to the docs site bundle. Mitigation: components are simple forms with no external dependencies beyond Vue itself. No charting libs, no heavy editors.
- **[State not persisted]** → Closing the browser tab loses work. Mitigation: the JSON can be copied/downloaded at any time. A future iteration could add localStorage persistence.
- **[No server-side validation]** → The builder validates structure and regex syntax but cannot verify that a ruleset actually matches Mediathek entries. Mitigation: the docs already explain the live test feature in the FunkArr UI.
