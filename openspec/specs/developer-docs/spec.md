# developer-docs Specification

## Purpose
TBD - created by archiving change developer-docs. Update Purpose after archive.
## Requirements
### Requirement: Developer group in the sidebar
The docs site SHALL provide a developer group as the last group of the existing sidebar in both locales, labelled "Für Entwickler" (German) and "For developers" (English), collapsed by default. The site SHALL NOT add a separate nav entry or a separate sidebar for it. The group SHALL list, in this order and without subgroups: Contributing (`develop/`), Architecture (`develop/architecture`), Repository layout (`develop/layout`), Building and testing (`develop/building`), Actors (`develop/actors`), Request flows (`develop/flows`), APIs (`develop/apis`), Web UI (`develop/web-ui`), Local development (`develop/local-development`), Development aids (`develop/dev-aids`), Rulesets (`develop/rulesets`), Releasing (`develop/releasing`).

#### Scenario: Group is collapsed on user pages
- **WHEN** a user opens `/getting-started`
- **THEN** the sidebar shows "Für Entwickler" as the last group, collapsed

#### Scenario: Group is open on developer pages
- **WHEN** a user opens `/en/develop/actors`
- **THEN** the sidebar shows the same user groups as on other pages, with "For developers" expanded and "Actors" active

#### Scenario: No separate navigation
- **WHEN** any page renders
- **THEN** the nav bar contains no entry for the developer pages

### Requirement: Repository link in the nav
The nav bar SHALL contain an item "GitHub" in both locales that links to `https://github.com/st0o0/funkarr` as an external link.

#### Scenario: Repository reachable from the nav
- **WHEN** any page renders, in German or English
- **THEN** the nav bar's last text item is "GitHub" and opens the repository

### Requirement: Developer pages exist in both locales
Every developer page SHALL exist under `docs/develop/` (German) and `docs/en/develop/` (English) with the same file name. Code, identifiers, file names and diagram labels SHALL stay English in both.

#### Scenario: Page parity
- **WHEN** the docs site is built
- **THEN** for every page under `/en/develop/` the same page exists under `/develop/`

### Requirement: Pages describe the system, not the conventions
Developer pages SHALL describe how FunkArr is structured, built, run, tested and released. They SHALL NOT restate code conventions. Where a convention is relevant, they SHALL link to the matching page on anvil.st0o0.net.

#### Scenario: Contributing page points to anvil
- **WHEN** a developer reads the Contributing page
- **THEN** it links to anvil.st0o0.net for code conventions and names AGENTS.md as the compact rule set used by agents

#### Scenario: No conventions catalog
- **WHEN** the developer pages are listed
- **THEN** none of them is a page of coding rules (C#, Akka, testing, API or metrics conventions)

### Requirement: Contributing page covers the change process
The Contributing page SHALL cover the ways to contribute (bug, feature, ruleset request, ruleset, code, docs) with links to the issue forms, the OpenSpec flow (explore, propose, apply, archive), Conventional Commits with the allowed types, and the checks to run before a pull request.

#### Scenario: Ruleset author finds the path
- **WHEN** a person without C# knowledge opens the Contributing page
- **THEN** they find a link to the Rulesets page without reading any code-related page

### Requirement: Commands run from the repository root
Commands on the developer pages SHALL run from the repository root (`--project`, `--dir`, `-f` instead of `cd`). Backend tests SHALL be shown with `dotnet run --project`, not `dotnet test`. The app SHALL be run through `docker-compose.dev.yml`, not `dotnet run` on the host project.

#### Scenario: Tests with dotnet run
- **WHEN** the Building and testing page shows how to run backend tests
- **THEN** it uses `dotnet run --project src/...Tests/...csproj`

### Requirement: Edit link on every page
Every docs page SHALL show an edit link to its source file on GitHub, labelled per locale.

#### Scenario: Edit link
- **WHEN** a user opens `/develop/actors`
- **THEN** the page shows an edit link to `docs/develop/actors.md` on the `main` branch

### Requirement: AGENTS.md points to anvil and the developer pages
AGENTS.md SHALL state near the top that code conventions with rationale and examples are at anvil.st0o0.net and should be consulted when unsure how to do something, and that `docs/develop/` describes how FunkArr is built.

#### Scenario: Agent looks up a pattern
- **WHEN** an agent reads AGENTS.md
- **THEN** it finds the anvil URL with the instruction to look there for how to implement something

