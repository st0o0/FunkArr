## Context

`test-assertion-hygiene` already codifies null-guard and collection-indexing conventions (`Assert.NotNull` before property access instead of `!.`, `Assert.Single` for one-element collections, count-before-index for multi-element access). Those rules prevent *crashes*; they say nothing about whether an assertion actually *proves* anything. Exploration surfaced two concrete gaps:

1. Three near-identical files (`RuleSetApiEndpointTests`, `RuleSetWriteApiEndpointTests`, `RuleSetTestApiEndpointTests`) each have one test that builds a `WebApplication`, calls `app.MapRuleSetApi()`, and asserts `IEndpointRouteBuilder.DataSources` is non-empty. This proves endpoints were registered somewhere, but nothing about which routes, methods, or response shapes exist.
2. In `EnrichmentManagerTests`, two of three tests do `ExpectMsg<T>()` followed by `Assert.NotNull(result)`. `ExpectMsg<T>()` already fails the test if no message of type `T` arrives within the timeout, so `result` can never be null at that point — the `Assert.NotNull` is a no-op that reads as a real check. The file's third test shows the fix already in practice: `Assert.Contains("not configured", response.Cause.Message)`.

Because a one-off grep on `Assert\.` undercounts coverage in Akka TestKit tests (ExpectMsg<T> is itself a strong assertion) and `Assert.NotNull` is sometimes the *correct*, convention-mandated null guard before further property asserts (per `test-assertion-hygiene`), this change needs a precise rule, not a blanket "add more asserts" instruction, and a per-file human judgment call during the audit.

## Goals / Non-Goals

**Goals:**
- Give `test-assertion-hygiene` two new, mechanically checkable-by-review rules: no vacuous post-ExpectMsg/post-guard assertion, and endpoint-registration tests must assert on actual request/response behavior.
- Fix the two seed cases as reference examples of the fix shape.
- Audit all test projects (including `FunkArr.IntegrationTests`) for the same two patterns and fix every instance found.

**Non-Goals:**
- Not a general "increase assert count" pass — files with already-thorough field-level assertions (e.g. `DownloadApiEndpointTests`, `ScoringActorTests`, `RemuxerTests`) are out of scope.
- Not touching `Assert.NotNull` usages that are legitimate null guards followed by real property assertions (those already satisfy both this and the existing spec).
- No production code changes; no new test infrastructure or helpers unless an endpoint test genuinely needs a minimal `WebApplicationFactory`-style harness to assert on a response (see Decisions).
- No change to assertion library/framework choice.

## Decisions

**How to fix the `RuleSet*ApiEndpointTests` trio.** Instead of building a bare `WebApplication` and inspecting `DataSources`, each test (or a consolidated single file, since all three currently assert the identical thing) should send an actual request through the mapped endpoints and assert on the HTTP response — reusing whatever lightweight test host pattern `FunkArr.Api.Tests` already has for other endpoint tests (check `MediathekApiEndpointTests.cs` for the existing in-repo pattern before introducing a new one). Decide per-file, during implementation, whether the three files are testing three distinct routes (keep three files, each asserting its own route's response) or the same route three times (consolidate into one file) — this requires reading `RuleSetApi.cs`'s actual endpoint mappings, which is implementation work, not a design decision to lock in now.

**How to fix `EnrichmentManagerTests`.** Replace `Assert.NotNull(stats)` with assertions on `CacheStatsResult`'s actual fields (counts), and replace `Assert.NotNull(response)` on `EnrichEpisodesFailed` with a `Assert.Contains("not configured", response.Cause.Message)`-style check, mirroring the third test in the same file exactly.

**Audit method.** Reuse the exploration's assert-count scan (asserts per `[Fact]`/`[Theory]`) as a triage signal only, then manually inspect every file surfaced with a low ratio to classify it as: (a) genuinely vacuous/registration-only — fix it; (b) thin because of `ExpectMsg<T>()` doing the proving — leave it; (c) thin because of convention-mandated null guards followed by real checks — leave it. Only (a) gets changed. This avoids a mechanical "add asserts everywhere" pass that would pad tests without improving them.

**Spec rule wording.** Add two `MODIFIED`-via-`ADDED` requirements to `test-assertion-hygiene` (additive, not replacing existing requirements) rather than a brand-new capability, since this is the same capability (assertion hygiene) gaining depth, not a new domain.

## Risks / Trade-offs

- [Risk] The audit could balloon scope across 10 test projects. → Mitigation: fix only instances matching the two documented patterns; anything else found during the audit gets noted but not fixed in this change (new finding → separate change).
- [Risk] Introducing a request/response harness for the RuleSet endpoint tests could require new test infrastructure if nothing reusable exists. → Mitigation: check `FunkArr.Api.Tests` for an existing minimal-host pattern first (`MediathekApiEndpointTests.cs`, `DownloadApiEndpointTests.cs` use extension-method-level testing without a full host — endpoint-level HTTP tests may need `WebApplicationFactory` or in-memory `TestServer`); if genuinely absent, add the smallest possible harness, not a general-purpose one.
- [Risk] Over-fixing: turning a legitimate `ExpectMsg<T>()`-only test into a padded test with assertions for assertions' sake. → Mitigation: the classification step in Decisions explicitly excludes case (b) and (c) from changes.

## Open Questions

- Do the three `RuleSet*ApiEndpointTests` files test the same route or three different ones? Needs to be answered by reading `RuleSetApi.cs` during implementation before deciding consolidate-vs-keep-three.
- Does `FunkArr.Api.Tests` already have a request/response test harness elsewhere (e.g. for `MediathekApiEndpointTests`) that can be reused, or does one need to be introduced?
