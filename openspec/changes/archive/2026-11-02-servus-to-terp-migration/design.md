## Context

FunkArr uses Servus 0.35.0 and Servus.Akka 0.3.15 for four things: the
AppBuilder startup with setup containers, the Akka registry helpers (GetActor,
GetActorAsync), BackoffOnFailure, and Servus.Resilience.Backoff. All have a
direct Terp counterpart. A detailed migration plan exists in MIGRATION.md at
the repo root with exact mapping tables, code snippets, and acceptance checks.

No persisted data or HOCON config references Servus, so there is no data
migration. The migration is purely a compile-time library swap.

## Goals / Non-Goals

**Goals:**
- Replace Servus/Servus.Akka with Terp/Terp.Akka across the entire codebase
- Adopt the Terp ISetup<T> hosting model with builder.Setups().Add<T>()
- Replace own ResolveChildActorExtensions with Terp.Akka's built-in version
- Replace ExternalApiMetricsHandler with TerpHttpMetricsHandler
- Replace ObservableGauge pattern with Gauge<T> (.NET 9+)
- Replace stream supervision boilerplate with .ResumeOnError()
- Update all documentation (specs, config.yaml) to reference Terp
- One commit per migration step for easy rollback

**Non-Goals:**
- Renaming setup container classes (XxxSetupContainer stays, Terp's XxxSetup
  naming is optional later)
- Startup gates (FunkArr doesn't use them)
- EntityRegionActor (FunkArr uses Cluster Sharding)
- Changing actor topology, message contracts, or persistence
- Dashboard/alert migration (documented in MIGRATION.md but executed separately)

## Decisions

### D1: Follow MIGRATION.md step order exactly

The MIGRATION.md plan defines 8 sequential steps with acceptance checks. Each
step is one commit. This order is chosen because step 3 (container rewrite) is
atomic — AkkaSetupContainer can only derive from the Terp base class once
Program.cs no longer feeds it to Servus's WithSetup.

**Alternative**: Big-bang single commit. Rejected — harder to rollback, harder
to review.

### D2: Use builder.Setups().Add<T>() chain API

The current Terp source uses a SetupChain pattern: `builder.Setups().Add<T>()`
returns a `SetupChain<TBuilder>` struct that preserves the concrete builder
type across the chain. The Terp documentation site still shows an older
`builder.Setup<T>()` API that no longer exists in the source. MIGRATION.md
correctly uses the chain API.

### D3: Include step 6 (metrics migration) and step 7 (ResumeOnError)

Step 6 changes external API metric names from `funkarr_external_api_*` to
`http_client_request_duration_seconds` with `api` tag. This is a breaking
change for dashboards. Step 7 replaces verbose stream supervision with
`.ResumeOnError()`. Both are included because the codebase is pre-1.0 and
dashboard migration can happen alongside.

### D4: Gauge<T> replaces ObservableGauge pattern

.NET 9+ provides `Gauge<T>` which is thread-safe and eliminates the backing
field + setter pattern. Five gauges across Download and RuleSet telemetry
classes are affected. No metric name change.

## Risks / Trade-offs

- **[Step 3 is atomic]** → All 11 containers + Program.cs change in one
  commit. Mitigation: the MIGRATION.md has exact before/after snippets and
  the change is mechanical.
- **[Metric name change in step 6]** → Dashboards querying
  `funkarr_external_api_*` break. Mitigation: MIGRATION.md documents PromQL
  equivalents. Update dashboards after deployment.
- **[Terp is 0.x]** → API may change. Mitigation: pin exact versions via CPM,
  Renovate proposes updates. Same author maintains both packages.
- **[Setup ordering is now explicit]** → A wrong order in builder.Setups() or
  app.Setups() is silent. Mitigation: MIGRATION.md documents the exact order
  and the reason for each position. Smoke test verifies endpoints.
- **[Terp docs outdated]** → llms-full.txt on terp.noneedconcept.org still
  shows the old Setup<T>() API. Mitigation: use source code as truth, not docs.

## Migration Plan

Follow MIGRATION.md steps 1-8 exactly:

1. Add Terp packages (keep Servus)
2. Namespace renames for helpers (GetActor, Backoff)
3. All containers + Program.cs (atomic)
4. Delete own ResolveChildActorExtensions
5. Remove Servus packages
6. Replace ExternalApiMetrics with TerpHttpMetricsHandler + Gauge<T>
7. Replace stream supervision with ResumeOnError
8. Update openspec/specs and openspec/config.yaml text references

Rollback: `git revert <commit>` per step in reverse order. Steps are
independent enough that partial rollback works.
