## Context

FunkArr already exposes a small, ad-hoc set of setup-testing endpoints
(`Configuration/SetupEndpoints.cs`: `/api/setup/status`, `/test-prowlarr`,
`/test-arr`, `/test-paths`, `/test-ffmpeg`, `/test-mediathek`) plus a
config read/write API (`/api/config`). Those endpoints answer "is X
reachable?" with a bare boolean and were built for an interactive
web-config editor.

MediathekArr's setup wizard goes further: for each Arr app it doesn't just
check reachability, it checks whether *FunkArr itself is correctly
registered* (as an indexer in Prowlarr, as a download client in
Sonarr/Radarr), and it explains what to click to fix a failure. That is
the gap this change closes. It is a **read-only diagnostic capability**,
independent of the existing config-editor endpoints, which are left
untouched (`Configuration/SetupEndpoints.cs` is out of scope for this
change).

## Goals / Non-Goals

**Goals:**
- Give users one endpoint that validates FunkArr's own health (API key,
  FFmpeg, writable paths) plus, when connection details are supplied,
  whether Prowlarr/Sonarr/Radarr are reachable *and* have FunkArr
  registered.
- Return structured, per-check results with fix guidance so the Web UI
  (built in a separate change) can render a checklist without needing
  its own interpretation logic.
- Run every check independently so one failure (e.g. Prowlarr
  unreachable) never prevents the rest of the report from being
  produced.

**Non-Goals:**
- No auto-configuration. The wizard never calls a Prowlarr/Sonarr/Radarr
  write endpoint (no `POST /api/v1/indexer`, no `PUT`, no `DELETE`). It
  is diagnostic only — the user still adds FunkArr manually.
- No persisted validation state, history, or scheduled re-checks. Every
  call to the endpoint is a fresh, stateless run.
- No changes to the existing `/api/setup/status` family or `/api/config`
  endpoints — they remain as-is for the config editor.
- No modification of `FunkArrOptions` persistence or actors.

## Decisions

### 1. Plain service, not an actor

**Decision:** Implement validation as `SetupValidationService`, an
ordinary DI-registered class with an `async Task<ValidationResult>
ValidateAsync(ValidationRequest, CancellationToken)` method — not an
Akka.NET actor.

**Rationale:** Every check is a stateless, request-scoped I/O call
(process spawn, filesystem probe, HTTP call). There is no shared mutable
state, no supervision requirement, and no reason to route through the
actor system. The project's own actor-hierarchy guidance
(`SearchActor`, `DownloadQueueActor`, `DownloadWorkerActor`,
`MuxingActor`) is reserved for components with lifecycle, persistence,
or concurrency-control needs — none of which apply here. Using
`IHttpClientFactory` and `Task.WhenAll` inside a plain service is simpler
and matches how `SetupEndpoints.cs` already does its own connectivity
checks.

### 2. Single endpoint: `POST /api/setup/validate`

**Decision:** One endpoint that accepts an optional set of Arr
connection details and runs the full battery of checks in one call,
rather than one endpoint per check (as `SetupEndpoints.cs` does today).

**Rationale:** The wizard is a single-screen checklist in the UI; the UI
needs one call to render the whole page, plus a natural way to re-run
individual sections by re-posting the same shape. A `POST` (not `GET`)
is required because the request body carries Arr API keys — not
appropriate as query parameters that end up in logs/URLs.

**Request shape:**
```json
{
  "prowlarr": { "url": "http://prowlarr:9696", "apiKey": "..." },
  "arrInstances": [
    { "name": "Sonarr", "type": "Sonarr", "url": "http://sonarr:8989", "apiKey": "..." },
    { "name": "Radarr", "type": "Radarr", "url": "http://radarr:7878", "apiKey": "..." }
  ],
  "selfUrl": "http://funkarr:9797"
}
```
- `prowlarr`, `arrInstances`, and `selfUrl` are all optional. Omitting
  `prowlarr`/`arrInstances` skips the corresponding external checks but
  self-checks always run.
- `selfUrl` is the address other Arr apps use to reach FunkArr. FunkArr
  cannot reliably infer this from `HttpContext` (reverse proxies,
  Docker networking), so the wizard UI asks the user for it up front and
  passes it through on every validate call. It is used only to improve
  registration-match confidence (see Decision 4); it is never persisted.
- None of `prowlarr.apiKey`, `arrInstances[].apiKey`, or `selfUrl` are
  written to `FunkArrOptions` or any config file by this endpoint — that
  remains the job of the existing `/api/config` `PUT` endpoint, which
  the user drives separately once the wizard confirms things look right.

**Response shape:**
```json
{
  "overallStatus": "warning",
  "checks": [
    {
      "category": "self",
      "name": "api-key",
      "status": "pass",
      "message": "API key is configured.",
      "fixGuidance": null
    },
    {
      "category": "prowlarr",
      "name": "prowlarr-registered",
      "status": "warning",
      "message": "Prowlarr is reachable but no indexer matching FunkArr was found.",
      "fixGuidance": "In Prowlarr, go to Settings > Indexers > Add Indexer > Newznab and add FunkArr using its Newznab URL and API key."
    }
  ]
}
```
`overallStatus` is derived, not independently computed: `fail` if any
check is `fail`, else `warning` if any check is `warning`, else `pass`.

### 3. Check result contract: name, status, message, fix guidance

**Decision:** Every check — self or external — returns the same
`CheckResult` shape: `Category`, `Name`, `Status` (`pass` | `warning` |
`fail`), `Message` (human-readable, states what was observed), and
`FixGuidance` (nullable, only populated for `warning`/`fail`, states the
concrete next action in the target application's own UI language, e.g.
"Settings > Indexers").

**Rationale:** A uniform contract lets the Web UI render every check with
one generic checklist-row component instead of special-casing self vs.
external checks. Fix guidance is deliberately actionable ("go here,
click this") rather than a restatement of the failure, mirroring
MediathekArr's wizard copy.

**Status semantics:**
- `pass` — check succeeded, nothing to do.
- `warning` — check partially succeeded (e.g. Arr app reachable but
  FunkArr not registered yet, or registered under a different name than
  expected) — actionable but not necessarily a broken deployment.
- `fail` — check could not complete or found a hard problem (e.g. Arr
  app unreachable, path not writable, API key empty).

### 4. Registration checks: heuristic match, not exact

**Decision:** "Is FunkArr registered?" is answered by fetching the
target app's list of indexers (Prowlarr: `GET /api/v1/indexer`) or
download clients (Sonarr/Radarr: `GET /api/v3/downloadclient`) and
scanning entries for one that plausibly points at this FunkArr instance:
1. **Name match** — an entry whose `name` contains `"funkarr"`
   (case-insensitive) is treated as a strong signal.
2. **Host match** (only when `selfUrl` was supplied) — an entry whose
   configured host/port (from its `fields` array — `baseUrl`/`apiPath`
   for Newznab indexers, `host`/`port` for SABnzbd-type download
   clients) matches the host/port parsed from `selfUrl` is the strongest
   signal and is required to reach `pass`.
3. If a name match is found but the host doesn't match `selfUrl` (or
   `selfUrl` wasn't supplied), the result is `warning` — "found something
   that looks like FunkArr, but couldn't confirm it points at this
   instance."
4. If neither matches, the result is `warning` (Arr app itself was
   reachable) with fix guidance to add FunkArr, not `fail` — the Arr
   integration itself is healthy, registration is just outstanding
   setup.

**Rationale:** Prowlarr/Sonarr/Radarr's indexer/download-client JSON
schemas don't have a stable, typed "this is a Newznab/SABnzbd client"
identity beyond `implementation` + free-form `fields`. Exact structural
matching is brittle across Arr app versions. A heuristic that degrades
gracefully to `warning` (rather than a hard `fail`/`pass` binary) keeps
the check honest about its own uncertainty and gives the user a
correct next step either way.

**Failure to reach the Arr app at all** (network error, 401, non-2xx) is
a separate, earlier check (`{app}-connectivity`) that short-circuits the
registration check for that app — if connectivity fails, the
registration check for that app is reported as `fail` with message
"skipped: could not connect to {app}" rather than attempting the lookup.

### 5. FFmpeg check reuses `FfmpegHealthCheck`

**Decision:** `SetupValidationService` takes a `FfmpegHealthCheck`
instance (or invokes it via DI) and maps its
`HealthCheckResult.Status` (`Healthy`/`Degraded`/`Unhealthy`) to
`pass`/`warning`/`fail`, using `HealthCheckResult.Description` as the
check `Message`.

**Rationale:** `Health/FfmpegHealthCheck.cs` already implements exactly
this probe (`ffmpeg -version` subprocess, exit-code check) for the
`/healthz` endpoint. Duplicating the subprocess logic in
`FunkArr.Setup` would drift over time; reusing the same `IHealthCheck`
keeps one source of truth for "is FFmpeg usable."

### 6. Partial-results execution model

**Decision:** `SetupValidationService.ValidateAsync` wraps each
individual check (self and external) in its own try/catch and runs all
of them concurrently via `Task.WhenAll` over a list of
`Task<CheckResult>`. An exception inside one check's implementation is
caught at that check's boundary and converted to a `fail` `CheckResult`
(`Message` = exception summary) rather than propagating and aborting the
whole request.

**Rationale:** This is the direct implementation of the "no side
effects, partial results" requirement — the wizard's whole value
proposition is showing the user everything that's wrong (or right) in
one pass, not stopping at the first problem.

### 7. Placement: new `FunkArr.Setup` namespace

**Decision:** New code lives under `Setup/` (namespace `FunkArr.Setup`):
`Setup/SetupValidationService.cs`, `Setup/SetupValidationModels.cs`
(request/response/check records), `Setup/SetupValidationEndpoints.cs`
(minimal API route), `Setup/ArrRegistrationChecker.cs` (Prowlarr +
Sonarr/Radarr registration lookups). Registered in
`FunkArrApplicationSetup` alongside the other `Map*Endpoints()` calls,
and the service registered in `FunkArrServiceSetup`.

**Rationale:** Follows the existing namespace-per-feature layering
(`Search/`, `DownloadClient/`, `Muxing/`, `Indexer/`). Keeping it
separate from `Configuration/SetupEndpoints.cs` avoids conflating the
new read-only validation capability with the pre-existing config-editor
endpoints, and avoids a route collision — this change's endpoint is
`POST /api/setup/validate`, distinct from the existing
`/api/setup/status` etc.

### 8. Authentication

**Decision:** `POST /api/setup/validate` sits behind the same
`apikey` query-parameter filter pattern used everywhere else
(`SetupApiKeyFilter` / `ApiKeyFilter`), reusing `SetupApiKeyFilter` from
`Configuration/SetupEndpoints.cs` rather than inventing a new filter.

**Rationale:** Consistency with every other FunkArr endpoint group
(Newznab, SABnzbd, match intelligence, ruleset, queue). No exception is
warranted just because this endpoint is diagnostic — it still reflects
configuration values (masked or not) and makes outbound calls with
user-supplied third-party API keys, so it should not be open.

## Risks / Trade-offs

- **Registration heuristics can misfire** (false `warning` when
  correctly registered under an unexpected name/host, or false
  confidence when another Newznab indexer happens to be named
  "funkarr"). Mitigated by capping registration confidence at `warning`
  unless the host match against `selfUrl` succeeds, and by writing fix
  guidance that's harmless to follow even if already done ("add FunkArr
  as an indexer" when it's already added just means the user confirms
  it's already there).
- **`selfUrl` is user-supplied and unvalidated** — a wrong value only
  degrades registration-check confidence, it never causes a false
  `fail`, so the blast radius of a bad `selfUrl` is limited to noisier
  `warning`s.
- **No caching of external calls** — each `/api/setup/validate` call
  re-hits Prowlarr/Sonarr/Radarr and re-spawns FFmpeg. Acceptable
  because this is a manually-triggered, low-frequency wizard action, not
  a polling endpoint.
- **`options-decomposition` dependency**: the proposal notes this change
  depends on that (not-yet-existing) change for clean access to config
  sections. This design does not require it — `SetupValidationService`
  only needs `IOptions<FunkArrOptions>` for the self-checks (API key,
  paths), the same access pattern `SetupEndpoints.cs` already uses. If
  `options-decomposition` lands first, `SetupValidationService` should
  switch to its narrower options interface instead of the full
  `FunkArrOptions`; if not, this change proceeds against `FunkArrOptions`
  directly with no blocking dependency.

## Migration Plan

No migration — purely additive. No existing routes, options, or
persisted data change shape. `Configuration/SetupEndpoints.cs` is
unmodified by this change.

## Open Questions

- Should `Configuration/SetupEndpoints.cs`'s overlapping checks
  (`/test-prowlarr`, `/test-arr`, `/test-ffmpeg`, `/test-paths`) be
  deprecated in favor of `/api/setup/validate` once the Web UI change
  adopts it? Left for the web-ui change to decide — out of scope here.
