# Search Edge Cases and Cache Invalidation

## Why

Five findings from the QA code review remain unaddressed:

1. **[Finding #10]** Both-type search returns partial results silently — callers cannot distinguish a partial result (one shard timed out) from a full result.
2. **[Finding #15]** MediathekViewWebManager crashes on StreamComplete, orphaning pending callers with no warning log of the pending count.
3. **[Finding #13]** VariantDetector does not normalize Unicode to NFC — decomposed forms (ä as a + combining diaeresis) do not match.
4. **[Finding #14]** VariantDetector regex patterns for OV/DGS need test coverage confirming no false positives.
5. **[Finding #9]** Newznab feed cache (OutputCache "feed" tag) is never evicted when new feed entries are published, causing up to 5 minutes staleness.

## What

- Add `IsPartial` flag to `SearchCommandCompleted` and set it when returning partial Both-search results
- Add warning log in MediathekViewWebManager.StreamComplete handler with pending request count
- Normalize input to NFC in VariantDetector.Detect before matching
- Add comprehensive VariantDetector edge case tests (OV/DGS word boundaries, NFD input)
- Evict the "feed" OutputCache tag when `PublishFeedEntries` adds new entries

## Scope

Search, ArrApi, Messages projects. No persistence changes. No breaking API changes (IsPartial is additive).
