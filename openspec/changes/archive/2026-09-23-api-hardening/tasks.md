## 1. Validation Infrastructure

- [x] 1.1 Create `SafeUrlAttribute` validation attribute in FunkArr.Api (validates http/https scheme, rejects loopback/private/link-local IPs after DNS resolution)
- [x] 1.2 Create `ValidationEndpointFilter` in FunkArr.Api that validates `[AsParameters]` and JSON body parameters via DataAnnotations, returns 400 `ValidationProblemDetails` on failure
- [x] 1.3 Register `ValidationEndpointFilter` on all five Minimal API endpoint groups (Mediathek, RuleSets, Downloads, System, Setup)

## 2. Request Model Annotations

- [x] 2.1 Add DataAnnotation attributes to `CreateRuleSetRequest`: `[Required]` on RuleSetId/Topic/Rules, `[RegularExpression]` on RuleSetId, `[MinLength(1)]` on Rules, `[Range(0,1)]` on Confidence
- [x] 2.2 Add DataAnnotation attributes to `UpdateRuleSetRequest`: `[Required]` on Topic/Rules, `[MinLength(1)]` on Rules, `[Range(0,1)]` on Confidence
- [x] 2.3 Add DataAnnotation attributes to `TestScoreRequest`: `[Range(0,1)]` on DefaultConfidence, `[Required]`+`[MinLength(1)]` on Rules and Candidates
- [x] 2.4 Add `[Required]` to `CreateArrResourceRequest.Url` and `ApiKey`, add `[SafeUrl]` to Url
- [x] 2.5 Add `[Range]` annotations to `MediathekSearchRequest`: Limit 1–100, Offset ≥ 0, DurationMin ≥ 0, DurationMax ≥ 0
- [x] 2.6 Add `[Range]` annotations to `DownloadHistoryRequest`: Start ≥ 0, Limit 1–1000
- [x] 2.7 Add `[Required]` on `PriorityRequest.Priority`, add `[Range]` on `MoveRequest.Position` (≥ 0)

## 3. Wire Dead Request Models

- [x] 3.1 Wire `MediathekSearchRequest` via `[AsParameters]` in `MediathekApiEndpoints.search` — replace 9 individual lambda params with single record parameter
- [x] 3.2 Wire `DownloadHistoryRequest` via `[AsParameters]` in `DownloadsApiEndpoints.history` — replace 3 individual lambda params with single record parameter
- [x] 3.3 Remove manual inline validation that is now covered by annotations (e.g. `Math.Clamp` on limit, `Math.Max` on offset) where the annotation enforces the same constraint

## 4. Error Handling Fixes

- [x] 4.1 Fix `EndpointExceptionFilter`: replace `ex.Message` in 500 response with "An unexpected error occurred", add ILogger and log the exception detail server-side
- [x] 4.2 Wrap `Ask<>` calls in `SabnzbdDownloadService` with try/catch: `TimeoutException` → `SabnzbdResult.Error("Request timed out", 504)`, other exceptions → `SabnzbdResult.Error` with generic message
- [x] 4.3 Wrap `Ask<>` calls in `SabnzbdQueueService` with try/catch: same pattern as 4.2
- [x] 4.4 Add response type guard in `SabnzbdQueueService.GetHistory()`: if response is not `HistoryResult`, return `SabnzbdResult.Error("History query failed", 502)`
- [x] 4.5 Fix `NewznabSearchService` catch block: differentiate `TimeoutException` (Warning, "Search timed out") from other exceptions (Error, "Search failed")

## 5. Auth Cleanup

- [x] 5.1 Store validated API key in `HttpContext.Items["ApiKey"]` from `ApiKeyActionFilter` base class after successful validation
- [x] 5.2 Update `NewznabController` to read API key from `HttpContext.Items["ApiKey"]` instead of `Request.Query["apikey"]`

## 6. Tests

- [x] 6.1 Add tests for `ValidationEndpointFilter` in FunkArr.Api.Tests: valid request passes, invalid request returns 400
- [x] 6.2 Add tests for `SafeUrlAttribute`: loopback rejected, private range rejected, public URL accepted, non-http scheme rejected
- [x] 6.3 Update `ApiKeyActionFilterTests` to verify API key is stored in `HttpContext.Items`
- [x] 6.4 Add tests for SABnzbd service timeout handling: verify `SabnzbdResult.Error` returned on `TimeoutException`
- [x] 6.5 Add test for `SabnzbdQueueService.GetHistory()` response type guard
- [x] 6.6 Add test for `NewznabSearchService` exception differentiation (timeout vs other)
- [x] 6.7 Run all test projects, verify build and format pass
