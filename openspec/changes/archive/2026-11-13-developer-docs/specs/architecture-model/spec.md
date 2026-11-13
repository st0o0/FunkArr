## ADDED Requirements

### Requirement: Model covers context, containers and actors
The repository SHALL contain a LikeC4 model under `docs/architecture/` that defines:
- external actors and systems: user, Sonarr, Radarr, Prowlarr, MediathekViewWeb, broadcaster CDNs, TMDB, TVDB, GitHub (community rulesets), Prometheus
- FunkArr containers: UI, Api, ArrApi, Search, Download, RuleSet, Scoring, Enrichment, History, Feed, journal, FFmpeg
- every actor listed in the AGENTS.md actor topology, placed in its domain container

#### Scenario: Actor topology parity
- **WHEN** the model is compared with the actor topology in AGENTS.md
- **THEN** every actor named there exists in the model in the same domain, and the model contains no actor absent from it

### Requirement: Actor roles are visible
Each actor element SHALL show its hosting role (singleton, shard region, local entity region, child or pool) and whether it is persistent, and these roles SHALL be visually distinguishable in the views.

#### Scenario: Singleton vs shard region
- **WHEN** a developer views a domain view
- **THEN** singletons and shard-region workers are distinguishable without reading their descriptions

### Requirement: Required views
The model SHALL define a landscape view (`index`), a container view (`containers`), one view per domain, and dynamic views `search-flow`, `download-flow`, `rss-sync` and `ruleset-sync`.

#### Scenario: Search flow
- **WHEN** the `search-flow` view is rendered
- **THEN** it shows the request path from the *arr app through ArrApi, the search actors, scoring and enrichment back to the Newznab response

### Requirement: Embedded views on the developer pages
The developer pages SHALL embed `index` and `containers` on Architecture, `search-flow`, `download-flow` and `rss-sync` on Request flows, and `ruleset-sync` on Rulesets, in both locales. Embedded diagrams SHALL be interactive with the view browser enabled, so every other view of the model is reachable from them. Views SHALL render client-side only, and pages without a view SHALL NOT load the diagram code.

#### Scenario: Static build succeeds
- **WHEN** the docs site is built
- **THEN** the build completes without rendering errors from embedded views

#### Scenario: No diagram code on unrelated pages
- **WHEN** a user loads a page without an embedded view (e.g. `/getting-started`)
- **THEN** no LikeC4 bundle is requested

#### Scenario: Domain view reachable
- **WHEN** a developer opens the view browser on the Architecture page
- **THEN** they can open the view of any domain

#### Scenario: Live update in dev
- **WHEN** a developer edits a `.c4` file while the docs dev server runs
- **THEN** the embedded diagram updates without restarting the server

### Requirement: Diagrams follow the site color scheme
Embedded views SHALL follow the docs site's light/dark mode.

#### Scenario: Dark mode
- **WHEN** a user switches the docs site to dark mode
- **THEN** embedded diagrams render in dark mode

### Requirement: Model is validated in CI
CI SHALL validate the model on pull requests that change `docs/architecture/**` or the docs dependencies, and SHALL fail on syntax errors, unresolved references or invalid views.

#### Scenario: Broken reference fails CI
- **WHEN** a pull request adds a relationship to a non-existent element
- **THEN** the model validation check fails

### Requirement: Model maintenance is part of the change process
AGENTS.md SHALL state that adding, removing or renaming an actor or domain requires updating the model and the Actors pages in both locales, and the pull request template SHALL contain a matching checklist item.

#### Scenario: New actor reminder
- **WHEN** a contributor opens a pull request
- **THEN** the template asks whether the architecture model was updated for actor or domain changes
