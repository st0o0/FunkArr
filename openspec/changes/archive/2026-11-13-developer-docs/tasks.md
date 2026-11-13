## 1. Carry over from docs/contributor-guide-v1

- [x] 1.1 LikeC4: `docs/architecture/**`, `docs/.vitepress/likec4-vite5.ts`, `docs/.vitepress/theme/components/ArchitectureView.vue`, theme registration, `docs/package.json` + lockfile (likec4 exact, react, react-dom)
- [x] 1.2 `.github/workflows/architecture-model.yml`, `.github/ISSUE_TEMPLATE/*`
- [x] 1.3 Move `E2E-TEST-PLAN.md` to `src/FunkArr.UI/e2e/` and update `.claude/skills/e2e-verify` paths

## 2. Site config

- [x] 2.1 Vite plugin (via shim) and `isCustomElement` for `likec4-*` in `docs/.vitepress/config.ts`
- [x] 2.2 Collapsed "Für Entwickler" / "For developers" group as last sidebar group in both locales (12 links, no nav entry)
- [x] 2.3 `editLink` per locale
- [x] 2.4 Nav item "GitHub" → repository in both locales

## 3. Pages (German and English, short and factual, conventions linked to anvil)

- [x] 3.1 `develop/index.md` (Contributing)
- [x] 3.2 `develop/architecture.md` with `index` and `containers` views
- [x] 3.3 `develop/layout.md`, `develop/building.md`
- [x] 3.4 `develop/actors.md`, `develop/flows.md` (search-flow, download-flow, rss-sync)
- [x] 3.5 `develop/apis.md`, `develop/web-ui.md`
- [x] 3.6 `develop/local-development.md`, `develop/dev-aids.md`
- [x] 3.7 `develop/rulesets.md` (ruleset-sync), `develop/releasing.md`

## 4. Repository files

- [x] 4.1 AGENTS.md: anvil pointer and `docs/develop/` pointer near the top; TestScoringWorker in the topology; enum-switch wording; drop the `FunkArr.ExternalApi` meter row; persistence-mapping file name; model-update rule for actor/domain changes; E2E plan path
- [x] 4.2 PR template with the checklist from the spec
- [x] 4.3 CONTRIBUTING.md stub linking `/en/develop/` and `/develop/`
- [x] 4.4 README: drop the test list, env table and hard-coded version; link to the configuration and developer pages

## 5. Verify

- [x] 5.1 `pnpm --dir docs build` passes (no dead links)
- [x] 5.2 DE/EN page parity under `develop/` checked by script
- [x] 5.3 Browser: collapsed group on user pages, open on developer pages, 6 embedded views render in light and dark mode, view browser opens a domain view, no LikeC4 request on `/getting-started`
- [x] 5.4 `pnpm --dir docs exec likec4 validate architecture`; actor parity AGENTS.md ↔ model
- [x] 5.5 `openspec validate developer-docs --strict`
