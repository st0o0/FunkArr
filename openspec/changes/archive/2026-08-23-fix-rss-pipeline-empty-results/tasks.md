## 1. Fix JSON Deserialization

- [ ] 1.1 Add `[JsonPropertyName]` attributes to `Url_Video`, `Url_Video_HD`, `Url_Video_Low`, `Url_Subtitle`, and `Url_Website` on `MediathekResultItem` in `MediathekClient.cs`
- [ ] 1.2 Add a deserialization test that verifies a sample MediathekViewWeb JSON response maps all URL fields correctly

## 2. URL Normalization

- [ ] 2.1 Add a static `NormalizeUrl` helper method (prepend `https:` to `//`-prefixed URLs, pass through absolute URLs unchanged)
- [ ] 2.2 Apply `NormalizeUrl` to `url` and `urlSubtitle` in `MatchingPipeline.CreateResult`
- [ ] 2.3 Apply `NormalizeUrl` to `url` and `urlSubtitle` in `QualityProbeService.ExpandWithProbingAsync` when constructing `SearchResult`
- [ ] 2.4 Add unit tests for `NormalizeUrl` (protocol-relative, absolute https, absolute http, empty/null)

## 3. HD Fallback Tier Fix

- [ ] 3.1 Change `ExpandWithProbingAsync` line 66 from `(item.Url_Video_HD, QualityTier.HD720)` to `(item.Url_Video_HD, QualityTier.HD1080)`
- [ ] 3.2 Update the existing `quality-probing` spec's "All phases fail" scenario to document HD1080 as the HD fallback tier

## 4. Verification

- [ ] 4.1 Run existing test suite and fix any test failures caused by the changes
- [ ] 4.2 Run `dotnet format` on changed files
