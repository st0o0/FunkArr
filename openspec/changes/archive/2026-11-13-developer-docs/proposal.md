## Why

FunkArr has no place for people who want to work on the code. Everything a developer needs sits in AGENTS.md, which is written for agents. A first attempt (branch `docs/contributor-guide-v1`) built a 15-page "Contributing" section with its own nav and sidebar, a "Conventions" group that restated AGENTS.md, and later a German mirror. Every rule then existed three times, and the section felt like a second site.

The model for this change is the developer section of docs.heavymatica.com: a collapsed "For developers" group at the bottom of the normal sidebar. It is a flat list of short pages, each about one part of the system or one task, with diagrams from a LikeC4 model. It contains no rule catalog. Code conventions already live at anvil.st0o0.net, which uses FunkArr as its reference implementation, so the docs link there and don't repeat them.

## What Changes

- New developer group in both locales: "Für Entwickler" (`/develop/`) and "For developers" (`/en/develop/`). It is a collapsed group at the bottom of the existing sidebar, with no extra nav tab and no separate sidebar.
- New nav item "GitHub" in both locales that links straight to the repository.
- 12 short pages per locale: Contributing, Architecture, Repository layout, Building and testing, Actors, Request flows, APIs, Web UI, Local development, Development aids, Rulesets, Releasing. Code conventions are linked to anvil, not written out.
- LikeC4 architecture model in `docs/architecture/`, embedded with an `ArchitectureView` component:
  - The pages show 6 views: the landscape, the containers and four flows.
  - The domain views stay in the model and can be opened through the diagram's view browser.
  - The model is validated in CI.
- Repository scaffolding:
  - issue forms (bug, feature, ruleset request) and a pull request template
  - CONTRIBUTING.md becomes a short stub that links to `/develop/`
  - the README is slimmed (no test list, no env table, no hard-coded version)
  - the manual E2E plan moves to `src/FunkArr.UI/e2e/`
- AGENTS.md:
  - points to anvil for "how do I do X" questions and to `docs/develop/` for how FunkArr is built
  - fixes stale facts (TestScoringWorker missing from the topology, the enum-switch wording, the `FunkArr.ExternalApi` meter row, the persistence-mapping file name)
  - adds the rule to update the model when actors change
  - it is not restructured otherwise

## Capabilities

### New Capabilities
- `developer-docs`: the developer group on the docs site, its pages, both locales, and the link to anvil.
- `architecture-model`: the LikeC4 model, its views, embedding and CI validation.
- `contribution-scaffolding`: CONTRIBUTING.md, README developer content, issue/PR templates, E2E plan location.

### Modified Capabilities
- `docs-i18n`: the sidebar of both locales gets the developer group.

## Impact

- `docs/develop/**`, `docs/en/develop/**` (new), `docs/architecture/**` (new), `docs/.vitepress/` (config, `ArchitectureView.vue`, Vite 5 shim), `docs/package.json` (likec4, react, react-dom)
- `.github/ISSUE_TEMPLATE/`, `.github/PULL_REQUEST_TEMPLATE.md`, `.github/workflows/architecture-model.yml`
- `AGENTS.md`, `CONTRIBUTING.md`, `README.md`, `E2E-TEST-PLAN.md` → `src/FunkArr.UI/e2e/`, `.claude/skills/e2e-verify`
- No application code changes.
