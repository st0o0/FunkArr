## Context

Sonarr and Radarr evaluate SABnzbd and Newznab responses for health checks during setup and ongoing operation. Several FunkArr response fields diverge from what real SABnzbd and Newznab servers return, causing visible warnings and swallowing error details.

All changes are output-only in the FunkArr.ArrApi project. No persistence, message, or domain changes.

## Goals / Non-Goals

**Goals:**

- Arr health checks pass without warnings after adding FunkArr as indexer/download client
- Error messages from both APIs reach the Arr UI with their specific text
- Numeric fields use invariant formatting regardless of container locale

**Non-Goals:**

- Category round-trip (echoing the raw category string back) — separate change `arr-core-fixes`
- Missing Newznab error codes 101-109, 203 — low value, deferred
- `Request limit reached` description for rate limiting detection — deferred
- Any changes to download or search behavior

## Decisions

### get_config: add history_retention_option and history_retention_number

Add two fields to `SabnzbdMiscConfig`:
- `history_retention_option: "all"` — Sonarr evaluates this first. The value `"all"` means "keep everything", which gives `false` for the "retention too short" check.
- `history_retention_number: 0` — secondary field, only evaluated for `"number-*"` and `"days-*"` options.

Keep the existing `history_retention: "all"` as a legacy fallback for older Arr versions.

**Why "all"?** FunkArr does not auto-delete history (the 1000-record trim is internal, not time-based). Claiming "all" is accurate. Sonarr's check: `"all" → false`, `"number-*"/"all-archive"/"all-delete" → true`, `"days-*" with n < 14 → true`.

### get_config: populate tv_categories and movie_categories

Build from `DownloadOptions.Categories`. Each configured category has a `MediaType`. Map to the arrays Sonarr/Radarr expect. This enables the `DownloadClientSortingCheck` to validate category configuration.

### All errors → HTTP 200

Both Newznab and SABnzbd protocols return errors as HTTP 200 with structured error bodies. Arr clients check the HTTP status code first; a non-200 hits the generic HTTP error handler and the specific error message is never shown.

Changes:
- `NewznabXmlResult.Error`: `StatusCode = 200` (was 403 for code 100, 400 for others)
- `SabnzbdController.MapResult` Error branch: `StatusCode = 200` (was `err.StatusCode`)
- `SabnzbdApiKeyFilter`: `StatusCode = 200` (was 403), message `"API Key Required"` (was `"API Key Incorrect"`)

**Why change the API key message?** Real SABnzbd says "API Key Required" when no key is provided and "API Key Incorrect" when the wrong key is given. FunkArr's filter doesn't distinguish — it says "Incorrect" for both. Using "Required" matches the more common case (misconfigured client). The distinction is cosmetic; the error body is what matters.

### mb/mbleft: invariant culture with one decimal

Change from `ToString("F0")` to `ToString("F1", CultureInfo.InvariantCulture)`:
- `"F0"` rounds to whole MB and uses the thread's current culture decimal separator
- `"F1"` with invariant culture gives consistent `"1234.5"` format everywhere
- Arr parses these as `decimal` — one decimal place is sufficient precision

## Risks / Trade-offs

- **[Risk] Arr behavior difference with HTTP 200 errors** → Unlikely negative. Arr clients parse the error body on 200 responses; they already handle this from real SABnzbd/Newznab servers. Switching from 4xx to 200 can only improve error message visibility.
- **[Risk] get_config field names wrong** → Verified against Sonarr 4/5 source code. Field names are `history_retention_option` and `history_retention_number` in the `SabnzbdConfig` model.
