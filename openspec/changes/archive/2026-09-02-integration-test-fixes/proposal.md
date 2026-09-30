## Why

First end-to-end test with Prowlarr/Sonarr/Radarr revealed two issues that break the integration experience: all results show as "Unknown" category in Prowlarr because the Newznab caps response declares no categories and results don't distinguish TV from movie categories, and entries from some broadcasters (notably ORF) show as "0 B" because MediathekViewWeb returns null for their size field.

## What Changes

- Populate Newznab caps response with supported categories (TV 5000 with HD/SD subcats, Movies 2000 with HD/SD subcats)
- Use correct Newznab category IDs based on search type: `t=tvsearch` → 5040/5030, `t=movie` → 2040/2030
- Estimate file size from duration and quality tier when MediathekViewWeb returns null size, instead of defaulting to 0

## Capabilities

### New Capabilities

- `size-estimation`: Estimate media file size from duration and quality tier when the upstream API returns no size data

### Modified Capabilities

- `newznab-indexer-api`: Add category declarations to caps response and use search-type-aware category mapping in results
- `mvw-response-model`: Handle nullable size field in API response model

## Impact

- `FunkArr.ArrApi` — Caps model, IndexerApiEndpoints (category mapping, search type context)
- `FunkArr.Search` — MediathekApiModels (already fixed: nullable size), size estimation logic
- `FunkArr.Messages` — MediathekItem may need nullable or estimated size propagation
