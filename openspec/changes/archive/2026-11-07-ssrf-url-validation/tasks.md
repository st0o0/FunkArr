## 1. URL Validation Helper

- [x] 1.1 Create `UrlValidation` static class in `FunkArr.ArrApi/Newznab/` with `HasAllowedScheme(string? url)` method — returns true only for http/https via `Uri.TryCreate` + scheme check

## 2. Apply Validation at Entry Points

- [x] 2.1 In `NzbPayload.Decode()`: validate `Url` after deserialization — return null if scheme not allowed
- [x] 2.2 In `NzbPayload.Decode()`: validate `SubtitleUrl` — clear to null if scheme not allowed (don't reject the whole payload)
- [x] 2.3 In `NzbService.FromNzb()`: validate the URL from `<meta type="funkarr-url">` — set to empty string if scheme not allowed

## 3. Tests

- [x] 3.1 Unit tests for `UrlValidation.HasAllowedScheme`: http ✓, https ✓, file ✗, ftp ✗, gopher ✗, null ✗, empty ✗, malformed ✗, no scheme ✗
- [x] 3.2 Unit test for `NzbPayload.Decode` with file:// URL returns null
- [x] 3.3 Unit test for `NzbPayload.Decode` with file:// SubtitleUrl clears subtitle but preserves payload
- [x] 3.4 Unit test for `NzbService.FromNzb` with non-http meta URL results in empty URL

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 4.3 Run ArrApi tests
