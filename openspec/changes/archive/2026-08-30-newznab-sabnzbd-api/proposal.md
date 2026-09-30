## Why

FunkArr needs two external API surfaces to integrate with the *arr ecosystem: a Newznab-compatible indexer API (so Prowlarr/Sonarr/Radarr can search for content) and a SABnzbd-compatible download client API (so Sonarr/Radarr can trigger and track downloads). These are the only interfaces that external tools interact with — without them FunkArr is invisible to the ecosystem. Both adapter projects exist as empty scaffolds.

## What Changes

- Implement complete Newznab XML indexer API in `FunkArr.IndexerApi` at `/index/api`
  - `?t=caps` — capabilities XML (search types, categories)
  - `?t=tvsearch` — TV search by tvdbid/season/episode or query string
  - `?t=search` — general text search
  - `?t=movie` — movie search by imdbid or query
  - `?apikey=` — authentication on all endpoints
  - Fake NZB download endpoint (URL encoded in NZB XML comments)
  - Full Newznab RSS XML response format with `newznab:attr` attributes
- Implement complete SABnzbd JSON download client API in `FunkArr.DownloadApi` at `/download/api`
  - `?mode=version` — SABnzbd version response
  - `?mode=get_config` — SABnzbd config with categories and complete_dir
  - `?mode=queue` — download queue with progress
  - `?mode=history` — download history with status
  - `?mode=history&name=delete` — delete history items
  - `?mode=addfile` — receive NZB POST, parse download URL, enqueue
  - `?apikey=` — authentication on all endpoints
- Add `DownloadPath` to `FunkArrOptions` for configuring the download directory
- Register endpoint mappings in `FunkArrApplicationSetup`
- Where Search and Download domains aren't built yet, adapters return valid but empty/stubbed responses — the wire format is correct even if no real data flows yet

## Capabilities

### New Capabilities
- `newznab-indexer-api`: Complete Newznab XML protocol implementation — caps, tvsearch, search, movie search, NZB download, RSS XML serialization, authentication
- `sabnzbd-download-api`: Complete SABnzbd JSON protocol implementation — version, config, queue, history, addfile, delete, authentication
- `api-authentication`: ApiKey query parameter validation middleware shared by both adapter APIs

### Modified Capabilities

## Impact

- `FunkArr.IndexerApi` — currently empty, gets Newznab endpoint + XML models
- `FunkArr.DownloadApi` — currently empty, gets SABnzbd endpoint + JSON models
- `FunkArr` host — `FunkArrApplicationSetup` gets endpoint mapping, `FunkArrOptions` gets `DownloadPath`
- `FunkArr.IndexerApi.Tests` — Newznab XML format tests, endpoint routing, auth
- `FunkArr.DownloadApi.Tests` — SABnzbd JSON format tests, endpoint routing, auth
- No domain project changes (Search, Download, RuleSet, MatchMagic untouched)
