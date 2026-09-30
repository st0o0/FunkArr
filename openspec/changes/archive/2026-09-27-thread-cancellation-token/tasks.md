## 1. FunkArr.Api - Minimal API endpoints

- [x] 1.1 Add `CancellationToken ct` to all endpoint lambdas in DownloadsApiEndpoints.cs, update all Ask calls to 3-arg overload (already done for SSE endpoint - do the rest)
- [x] 1.2 Add `CancellationToken ct` to all endpoint lambdas in RuleSetApiEndpoints.cs, update all Ask calls
- [x] 1.3 Add `CancellationToken ct` to all endpoint lambdas in MediathekApiEndpoints.cs, update all Ask calls
- [x] 1.4 Add `CancellationToken ct` to all endpoint lambdas in SystemApiEndpoints.cs, update all Ask calls
- [x] 1.5 Build FunkArr.Api, verify no errors

## 2. FunkArr.ArrApi - Controllers

- [x] 2.1 Add `CancellationToken cancellationToken` to NewznabController action methods, pass to service
- [x] 2.2 Add `CancellationToken cancellationToken` to SabnzbdController action methods, pass to service

## 3. FunkArr.ArrApi - Service classes

- [x] 3.1 Add `CancellationToken` parameter to NewznabSearchService/NzbService public methods, update Ask calls
- [x] 3.2 Add `CancellationToken` parameter to SabnzbdDownloadService public methods, update Ask calls
- [x] 3.3 Add `CancellationToken` parameter to SabnzbdQueueService public methods, update Ask calls
- [x] 3.4 Build FunkArr.ArrApi, verify no errors

## 4. Tests

- [x] 4.1 Update FunkArr.ArrApi.Tests: pass `CancellationToken.None` to service method calls in NzbServiceTests and any other affected tests
- [x] 4.2 Update FunkArr.Api.Tests: if any integration tests call endpoints directly, verify CT parameter handling
- [x] 4.3 Run all test projects, verify all pass

## 5. Verification

- [x] 5.1 Run full solution build: `dotnet build src/FunkArr.slnx`
- [x] 5.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Grep for remaining `Ask<` calls in Api/ArrApi without CancellationToken to confirm none were missed
