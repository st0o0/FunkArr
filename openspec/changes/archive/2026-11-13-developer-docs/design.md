## Context

- Docs: VitePress 1.6 with German as root locale and English under `/en/`. Theme components include the RulesetBuilder, and the ruleset catalog uses VitePress data loaders. The site is deployed by the shared `st0o0/github-workflows/docs.yml`, which expects `.vitepress/dist`.
- Code conventions with rationale live at anvil.st0o0.net (MkDocs), written from FunkArr. AGENTS.md is the compact rule set for agents.
- Reference: docs.heavymatica.com/develop/. It has a collapsed group, a flat list, short factual pages, and an architecture page with one diagram per part.
- A previous attempt is kept on `docs/contributor-guide-v1`. Its LikeC4 model, `ArchitectureView`, Vite 5 shim, CI workflow, GitHub templates and AGENTS.md fixes are reused. Its page content is not.

## Goals / Non-Goals

**Goals:**
- A developer finds how FunkArr is built, run, tested and released, without a second site and without a rule catalog.
- Every page exists in German and English and stays short enough that keeping both is cheap.
- Each rule lives in one place: anvil (with rationale) and AGENTS.md (compact, for agents). The docs link to them.

**Non-Goals:**
- Restating conventions (C#, Akka, testing, API, metrics) on the docs site.
- Reference pages generated from code (config, metrics).
- Moving to Astro Starlight.
- Restructuring AGENTS.md beyond the stale-fact fixes and the anvil pointer.

## Decisions

**D1: Collapsed group at the bottom of the normal sidebar.** The developer group is added to the `/` and `/en/` sidebars as `{ text: 'Für Entwickler' | 'For developers', collapsed: true, items: [...] }`. There is no nav tab and no path-keyed sidebar. VitePress opens a collapsed group when the current page is inside it. *Alternative:* a separate sidebar under `/develop/` (the v1 approach). Rejected, because it turns the section into a second site. The nav does get one external item, "GitHub", pointing to the repository in both locales; it complements the `socialLinks` icon, which is easy to miss and is hidden in the mobile menu.

**D2: Paths `/develop/` and `/en/develop/`, flat.** The page list is flat with no subgroups. `develop/index.md` is the Contributing page, so the group's first link is `/develop/`. Slugs are English in both locales (`architecture`, `layout`, `building`, `actors`, `flows`, `apis`, `web-ui`, `local-development`, `dev-aids`, `rulesets`, `releasing`). Only titles and labels are translated.

| Page | Content | Embedded views |
|---|---|---|
| Contributing (`/develop/`) | ways to contribute, OpenSpec flow, Conventional Commits, before a PR, link to anvil and AGENTS.md | - |
| Architecture | what FunkArr is, landscape, domains and reference direction, editing the model | `index`, `containers` |
| Repository layout | `src/` projects, `data/community`, `docs/`, `openspec/` | - |
| Building and testing | requirements, build, tests via `dotnet run`, UI tests, docs site, CI workflows | - |
| Actors | hosting shapes, topology tree, persistent actors, how actors are resolved | - (domain views via the view browser) |
| Request flows | search, download, RSS crawl, with the non-obvious facts under each | `search-flow`, `download-flow`, `rss-sync` |
| APIs | `/api`, `/index/api`, `/download/api`, *arr protocol fidelity | - |
| Web UI | `src/FunkArr.UI`: dev server and proxy, i18n, tests | - |
| Local development | `docker-compose.dev.yml`, pre-wired *arr apps, API key, ports, proxy profile, TMDB/TVDB keys | - |
| Development aids | ruleset debugger, log stream, `/metrics`, manual E2E plan, `e2e-verify` skill | - |
| Rulesets | community ruleset files, schema check, PR conventions, separate release | `ruleset-sync` |
| Releasing | release-please, version policy (0.x), rulesets component, image, docs deploy | - |

**D3: Page style.** Each page is about one screen of short, factual sentences: where it is, how to run it, what is surprising. Where a convention matters, link to the anvil page (e.g. `akka/persistence/`) instead of explaining it. German uses "du", like the user docs. Code, identifiers and diagram labels stay English.

**D4: Anvil and AGENTS.md as the rule sources.** The Contributing page links anvil for code conventions and names AGENTS.md as the agent version. AGENTS.md gets a short note near the top: conventions with rationale and examples are at anvil.st0o0.net, look there when unsure how to do something. `docs/develop/` describes how FunkArr is built.

**D5: LikeC4 integration as in v1.** These parts are reused unchanged from `docs/contributor-guide-v1`:
- the model in `docs/architecture/` (spec, model, views, dynamic, 13 views)
- `LikeC4VitePlugin` wrapped by the Vite 5 hook-filter shim
- `ArchitectureView.vue`: client-only, lazy `likec4:webcomponents/funkarr`, follows dark mode
- `likec4` pinned exactly
- the path-filtered `architecture-model.yml` workflow

Dynamic views use `autoLayout TopBottom` because the content column is narrow. Only 6 views are embedded. The rest are reachable through the view browser (`browser="true"`).

**D6: Stay on VitePress.** Starlight would remove the 40-line shim and add anchor checking. A migration would cost the RulesetBuilder (9 Vue components), the catalog data loaders, 30 user pages with containers, the theme CSS, and a change to the shared docs workflow. The structure this change needs is available in VitePress (`collapsed`, `editLink`, prev/next).

**D7: Edit link.** Enable `themeConfig.editLink` (per-locale text) pointing to `github.com/st0o0/funkarr/edit/main/docs/:path`, so every page has an edit link, as on Heavymatica.

## Risks / Trade-offs

- [DE and EN drift] → pages are short and factual, the PR template asks for both locales, and the build fails on dead links.
- [Anvil and FunkArr drift apart] → anvil is the reference. The developer pages only describe FunkArr's structure, so a convention change touches anvil and AGENTS.md, not the docs.
- [Model drifts from code] → AGENTS.md rule, PR checklist item, CI validation.
- [Vite 5 shim breaks on a LikeC4 update] → `likec4` is pinned exactly. Remove the shim when VitePress runs on Vite ≥ 6.3.

## Migration Plan

Docs and repo files only. Reusable files are taken from `docs/contributor-guide-v1` with `git checkout docs/contributor-guide-v1 -- <path>`. Delete the v1 branch after merge.
