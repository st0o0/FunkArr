## Context

FunkArr is a headless .NET service with two machine-to-machine API surfaces (Newznab XML for indexers, SABnzbd JSON for download clients). Configuration requires editing `appsettings.json` or environment variables. Ruleset authoring means editing JSON files on disk. There is no way to see download progress, inspect match failures, or test rule changes without checking logs.

The existing backend already has the data: `DownloadQueueActor` tracks jobs, `MatchLedgerActor` tracks match traces, `RuleSetRegistryActor` manages rulesets across three layers. The UI mostly needs to surface existing state and add CRUD + test endpoints for rulesets and config.

## Goals / Non-Goals

**Goals:**

- Provide a functional web UI for monitoring downloads, managing rulesets, and configuring arr connections
- Support first-run setup wizard for users new to the arr ecosystem
- Enable live ruleset testing against real Mediathek data
- Keep the UI simple, tool-like, and dependency-light
- Work in Docker without additional services

**Non-Goals:**

- Real-time WebSocket push (polling is sufficient for download progress)
- User accounts or multi-user auth (single API key, same as existing APIs)
- Mobile-first design (desktop browser is the primary target)
- Replacing the SABnzbd/Newznab APIs (those remain for arr integration)
- Managing Sonarr/Radarr series/movies from FunkArr's UI

## Decisions

### D1: Vue 3 SPA with Vite + Tailwind in `src/FunkArr.UI/`

**Choice:** Vue 3 (Composition API) + Vite + Tailwind CSS, source in `src/FunkArr.UI/`, build output to `src/FunkArr/wwwroot/`.

**Alternatives considered:**
- *Embedded static HTML + vanilla JS*: Sufficient for queue/history but painful for the ruleset editor's nested filter groups and wizard step management.
- *Razor Pages*: Server-rendered, no JS framework needed, but more friction for interactive components (filter builder, drag-and-drop) and doesn't separate frontend concerns.
- *Blazor*: Too heavy for a simple dashboard. Adds significant bundle size and complexity.

**Rationale:** Vue 3 is lightweight, has excellent TypeScript support, and the Composition API keeps components small. Tailwind avoids a custom design system while staying utility-first (easy to keep "un-fancy"). The separate `FunkArr.UI` directory keeps frontend concerns out of the .NET project.

### D2: Static file serving with SPA fallback

**Choice:** ASP.NET serves the Vite build output from `wwwroot/` using `UseStaticFiles` + SPA fallback. In development, Vite dev server runs on `:5173` with a proxy to the .NET backend on `:5000`.

**Rationale:** Zero runtime frontend dependencies. The .NET host serves the built assets like any other static file. No Node.js needed at runtime. The Vite dev proxy gives hot-reload during development.

```
Production:                     Development:
Browser → :5000                 Browser → :5173 (Vite HMR)
  ├── /api/*  → Minimal API       ├── /api/*  → proxy to :5000
  └── /*      → wwwroot/          └── /*      → Vite dev server
```

### D3: Config persistence via `data/config.json`

**Choice:** Runtime config writes go to `data/config.json`, separate from `appsettings.json`. The .NET host loads both at startup with `config.json` taking precedence.

**Alternatives considered:**
- *Write to appsettings.json directly*: Risky in Docker (container filesystem is ephemeral unless volume-mounted). Mutating the app's own config file at runtime is fragile.
- *Environment variables only*: Can't be written at runtime.

**Rationale:** `data/` is already volume-mounted in Docker for persistence (SQLite, rulesets). Adding `config.json` there keeps all mutable state in one place. The wizard writes to `config.json`; `appsettings.json` remains the immutable base layer.

```csharp
builder.Configuration.AddJsonFile(
    Path.Combine(options.DataPath, "config.json"),
    optional: true, reloadOnChange: true);
```

### D4: Two wizard paths — with and without Prowlarr

**Choice:** The setup wizard asks the user whether they use Prowlarr. This determines the instructions shown:

- **With Prowlarr**: FunkArr is added as a Newznab indexer in Prowlarr (which syncs to Sonarr/Radarr). Sonarr/Radarr only need FunkArr as a download client.
- **Without Prowlarr**: FunkArr is added directly in Sonarr/Radarr as both indexer AND download client.

**Rationale:** Both setups are common in the arr ecosystem. Showing only one path confuses users in the other camp. The wizard stores Prowlarr/Sonarr/Radarr connection details for the settings page to show connection status, but FunkArr itself doesn't actively use them — the arr apps call FunkArr, not the other way around.

### D5: Arr connection details are for testing and status display only

**Choice:** Prowlarr/Sonarr/Radarr URLs and API keys stored in config are used exclusively for:
1. Wizard/settings "Test Connection" buttons
2. Settings page connection status indicators

FunkArr never initiates calls to arr apps during normal operation.

**Rationale:** FunkArr is a passive service (indexer + download client). The arr apps query FunkArr. Storing their credentials is purely a UX convenience for setup verification, not a functional dependency.

### D6: Ruleset test endpoint runs the real matching engine

**Choice:** `POST /api/rulesets/test` accepts a topic name, optional TVDB ID, and an array of rules. It searches the Mediathek for the topic, fetches TVDB episodes if a TVDB ID is provided, then runs `RuleSetMatchingEngine.EvaluateRulesWithTraces` and returns the full trace output.

**Rationale:** Reusing the existing matching engine guarantees test results match production behavior exactly. No separate test logic to maintain. The trace infrastructure already exists — `MatchedTrace`, `FilteredTrace`, `UnmatchedTrace` give the UI everything it needs.

### D7: Polling for queue updates, not WebSockets

**Choice:** The queue view polls `GET /api/queue` every 2-3 seconds when visible.

**Alternatives considered:**
- *Server-Sent Events*: Lower latency but adds complexity (connection management, reconnection).
- *WebSockets via SignalR*: Full-duplex overkill for one-way status updates.

**Rationale:** Queue view is not latency-sensitive (progress bars updating every 2-3s is fine). Polling is trivially simple, works through all proxies, and the endpoint is cheap (in-memory actor state). A `usePolling` composable handles the interval and pauses when the tab is hidden.

### D8: Vue Router with hash mode

**Choice:** Use Vue Router in hash mode (`/#/rulesets/tatort`) rather than history mode.

**Rationale:** Hash mode requires zero server-side routing configuration. No SPA fallback catch-all needed. Works with any reverse proxy without rewrite rules. The URL aesthetics don't matter for a self-hosted tool.

### D9: Ruleset CRUD via new RuleSetRegistryActor messages

**Choice:** New actor messages for list, get-single, save-local, delete-local, and reload operations. The API endpoint handler asks the actor; the actor manages file I/O and in-memory index updates.

**Rationale:** The registry actor already owns the in-memory index and file system state. Routing CRUD through it avoids race conditions between HTTP handlers writing files and the actor reading them. All mutation flows through one place.

### D10: Multi-stage Docker build

**Choice:** Dockerfile adds a Node.js stage before the .NET build stage:
1. Node stage: `npm ci && npm run build` in `src/FunkArr.UI/`
2. .NET stage: copies Vite output into `src/FunkArr/wwwroot/` before `dotnet publish`

**Rationale:** Single Dockerfile, single `docker build` command. Node.js is only a build dependency, not present in the final image. The chiseled aspnet base image stays lean.

## Risks / Trade-offs

**[Risk] Config.json and appsettings.json precedence confusion** → Document clearly that `data/config.json` overrides `appsettings.json`. Log a warning at startup when both define the same key.

**[Risk] Ruleset test endpoint is slow for topics with many Mediathek results** → The Mediathek search and TVDB lookup add latency. Show a spinner in the UI. Consider caching recent Mediathek results in the SearchActor (already has caching).

**[Risk] Tailwind CSS purge misses dynamic classes** → Use Tailwind's safelist for dynamically constructed classes (status colors). Prefer full class names over string concatenation.

**[Risk] Frontend build breaks .NET CI** → Make the Vite build a separate CI step. The .NET build should succeed without `wwwroot/` (development mode). Only Docker build requires both.

**[Trade-off] Hash-mode URLs are less clean** → Accepted. For a self-hosted Docker tool, `/#/rulesets` is fine. Avoids all SPA fallback complexity.

**[Trade-off] Polling adds load vs push** → Accepted. One `GET /api/queue` every 3 seconds is negligible. The actor responds from memory, no DB queries.

## Open Questions

- Should the Vite build output (`wwwroot/`) be committed to git or only produced in CI/Docker? Leaning toward gitignored + CI-built.
- Should the wizard be able to auto-register FunkArr in Prowlarr/Sonarr via their APIs, or only show copy-paste instructions? Starting with instructions only, auto-register as a later enhancement.
