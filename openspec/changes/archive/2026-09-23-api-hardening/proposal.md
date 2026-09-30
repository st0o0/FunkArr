## Why

A full API audit revealed 8 findings across validation, security, error handling, and dead code. No request model uses DataAnnotations, no `ModelState.IsValid` checks exist anywhere, SABnzbd services have unhandled `TimeoutException` paths, and `CreateArrResourceRequest.Url` is an SSRF vector. These should be addressed together since they touch overlapping files and share common patterns.

## What Changes

- Add DataAnnotation validation attributes (`[Required]`, `[Range]`, `[Url]`) to all request models across both API projects
- Create a `ValidationEndpointFilter` for Minimal APIs to enforce annotation-based validation before handlers run
- Fix unhandled `TimeoutException` in SABnzbd services — wrap `Ask<>` calls, translate to `SabnzbdResult.Error`
- Fix Newznab search swallowing all exceptions as "Search timed out" — differentiate timeout vs other errors
- Stop leaking `ex.Message` in `EndpointExceptionFilter` — use generic message, log detail server-side
- Add response type guard in `SabnzbdQueueService.GetHistory()`
- Fix SSRF: validate `CreateArrResourceRequest.Url` against loopback/private ranges
- Wire dead `MediathekSearchRequest` via `[AsParameters]` or delete it
- Wire dead `DownloadHistoryRequest` via `[AsParameters]` or delete it
- Pass validated API key through `HttpContext.Items` instead of double-reading from query string

## Capabilities

### New Capabilities

- `api-validation`: DataAnnotation-based validation for all request models with a Minimal API validation filter

### Modified Capabilities

- `error-handling-standards`: Fix exception leaking, add missing try/catch in SABnzbd services, differentiate error types in Newznab search
- `api-request-models`: Wire or remove dead models, add validation annotations to all request records
- `api-authentication`: Pass API key through HttpContext.Items instead of double-reading from query string
- `arrapi-service-results`: Add timeout handling in SABnzbd services, add response type guard in GetHistory()

## Impact

- **FunkArr.Api**: EndpointExceptionFilter, all endpoint group files (new filter registration), request models in Models/
- **FunkArr.ArrApi**: NewznabController, SabnzbdController, NewznabSearchService, NzbService, SabnzbdDownloadService, SabnzbdQueueService, ApiKeyActionFilter, request records
- **FunkArr.ArrApi.Tests**: Update tests for new validation behavior, add tests for error handling paths
- **FunkArr.Api.Tests**: Add tests for validation filter
- **No breaking API changes**: validation adds 400 responses for invalid input that previously would have produced undefined behavior
