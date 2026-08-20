## 1. Models

- [ ] 1.1 Create `Setup/SetupValidationModels.cs` with `ValidationRequest`
      (optional `ArrConnection? Prowlarr`, `List<ArrInstanceConnection>
      ArrInstances`, `string? SelfUrl`) — reuse the existing
      `ArrConnection`/`ArrInstanceConnection` types from
      `FunkArr.Configuration`
- [ ] 1.2 Define `CheckStatus` enum: `Pass`, `Warning`, `Fail` (serialize
      as lowercase strings: `pass`/`warning`/`fail`)
- [ ] 1.3 Define `CheckResult` record: `Category`, `Name`, `Status`,
      `Message`, `FixGuidance` (nullable)
- [ ] 1.4 Define `ValidationResult` record: `OverallStatus`,
      `IReadOnlyList<CheckResult> Checks`, with a helper to derive
      `OverallStatus` from the check list (fail > warning > pass)

## 2. Self-check validators

- [ ] 2.1 Implement `api-key` check: `FunkArrOptions.ApiKey` non-empty
      → pass, else fail with fix guidance for `FunkArr__ApiKey`
- [ ] 2.2 Implement `ffmpeg` check: invoke `FfmpegHealthCheck` and map
      `HealthCheckResult.Status`/`Description` to `CheckResult`
      (Healthy→pass, Degraded→warning, Unhealthy→fail)
- [ ] 2.3 Implement `download-path` check: create-if-missing +
      write/delete test file against `FunkArrOptions.DownloadPath`
- [ ] 2.4 Implement `temp-path` check: create-if-missing + write/delete
      test file against `FunkArrOptions.TempPath`, independent of 2.3
- [ ] 2.5 Wrap each self-check in try/catch converting unexpected
      exceptions to a `fail` `CheckResult` rather than throwing

## 3. Prowlarr validation

- [ ] 3.1 Implement `prowlarr-connectivity` check: `GET
      {url}/api/v1/health` with `X-Api-Key` header, timeout ~10s;
      pass on 2xx, fail with fix guidance otherwise (network error,
      timeout, non-2xx)
- [ ] 3.2 Implement `prowlarr-registered` check: on connectivity pass,
      `GET {url}/api/v1/indexer`, scan entries for a name match
      (case-insensitive contains "funkarr") and, when `SelfUrl` was
      supplied, a host/port match against the indexer's `fields`
      (`baseUrl`/`apiPath`)
- [ ] 3.3 Apply match confidence rules from design.md §4: host+name
      match → pass; name-only match → warning (unconfirmed); no match →
      warning (not registered, with fix guidance); connectivity failure
      → skip with fail status and "skipped" message
- [ ] 3.4 Skip both Prowlarr checks entirely (omit from result) when the
      request has no `Prowlarr` section

## 4. Sonarr/Radarr validation

- [ ] 4.1 Implement `{instance}-connectivity` check per
      `ArrInstanceConnection`: `GET {url}/api/v3/system/status` with
      `X-Api-Key` header, timeout ~10s; pass on 2xx, fail otherwise
- [ ] 4.2 Implement `{instance}-registered` check: on connectivity pass,
      `GET {url}/api/v3/downloadclient`, scan entries for a name match
      (case-insensitive contains "funkarr") and, when `SelfUrl` was
      supplied, a host/port match against the client's `fields`
      (`host`/`port`)
- [ ] 4.3 Apply the same match confidence rules as Prowlarr (§3.3),
      producing checks named using the instance's configured `Name` so
      multiple Sonarr/Radarr instances don't collide
- [ ] 4.4 Run instance checks independently — one instance's failure
      must not affect another's checks or short-circuit the batch
- [ ] 4.5 Skip Sonarr/Radarr checks entirely when `ArrInstances` is
      empty or omitted

## 5. Orchestration service

- [ ] 5.1 Create `Setup/SetupValidationService.cs` with
      `ValidateAsync(ValidationRequest, CancellationToken)` returning
      `ValidationResult`
- [ ] 5.2 Always include the four self-checks (2.1–2.4); conditionally
      include Prowlarr checks (3.x) and per-instance Arr checks (4.x)
      based on what the request supplies
- [ ] 5.3 Run all applicable checks concurrently via `Task.WhenAll`
      over per-check `Task<CheckResult>`, each independently
      try/catch-wrapped so one check's exception cannot fail the batch
- [ ] 5.4 Compute `OverallStatus` from the aggregated `CheckResult` list
- [ ] 5.5 Register `SetupValidationService` (and `FfmpegHealthCheck` as
      a resolvable dependency, if not already registered for DI use
      beyond the health-check middleware) in `FunkArrServiceSetup`

## 6. Endpoint

- [ ] 6.1 Create `Setup/SetupValidationEndpoints.cs` with
      `MapSetupValidationEndpoints(this WebApplication app)`
- [ ] 6.2 Map `POST /api/setup/validate`, applying the existing
      `SetupApiKeyFilter` (from `FunkArr.Configuration`) for `apikey`
      query-parameter authentication
- [ ] 6.3 Bind request body to `ValidationRequest` (treat missing/empty
      body as "self-checks only" — not a 400)
- [ ] 6.4 Call `SetupValidationService.ValidateAsync` and return the
      `ValidationResult` as JSON with a 200 status regardless of
      individual check outcomes
- [ ] 6.5 Register `app.MapSetupValidationEndpoints()` in
      `FunkArrApplicationSetup` alongside the other `Map*Endpoints()`
      calls

## 7. Unit tests

- [ ] 7.1 Self-checks: api-key pass/fail, ffmpeg pass/warning/fail
      (mock/stub `FfmpegHealthCheck` result), download-path and
      temp-path pass/fail (use a temp directory and a deliberately
      non-writable path)
- [ ] 7.2 Prowlarr: connectivity pass/fail (mocked `HttpMessageHandler`
      via `IHttpClientFactory`), registration pass (name+host match),
      warning (name-only match), warning (no match), fail (connectivity
      failure skips registration check)
- [ ] 7.3 Sonarr/Radarr: same matrix as 7.2 per instance, plus a
      multi-instance test asserting one instance's failure doesn't
      affect another's results
- [ ] 7.4 Orchestration: request with no Arr sections → only self-checks
      in result; request with all sections → full check set; one check
      throwing → still returns full partial result with that check as
      `fail`
- [ ] 7.5 `OverallStatus` derivation: all-pass → pass; any warning,
      no fail → warning; any fail → fail

## 8. Integration test

- [ ] 8.1 `POST /api/setup/validate` without `apikey` → 401
- [ ] 8.2 `POST /api/setup/validate?apikey=<valid>` with empty body →
      200 with only self-check results
- [ ] 8.3 `POST /api/setup/validate?apikey=<valid>` with a full request
      body (using a local test HTTP handler standing in for
      Prowlarr/Sonarr/Radarr) → 200 with self + external check results
- [ ] 8.4 Assert response is always 200 even when an external check
      fails (never propagates as 5xx)

## 9. Verification

- [ ] 9.1 `dotnet build FunkArr.slnx` from `src/` passes
- [ ] 9.2 `dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj`
      passes, including new Setup validator, service, and endpoint
      tests
