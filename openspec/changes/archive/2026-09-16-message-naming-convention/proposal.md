## Why

FunkArr.Messages has no formal naming convention for commands, queries, and responses. Each domain evolved its own pattern: Search uses `*Command` suffixes, Download uses bare verbs, RuleSet/Scoring mixes both. Responses are inconsistent too — `*Completed`/`*Failed` in Search, `*Result` in Download, both in Scoring. This makes the codebase harder to navigate and new messages harder to name correctly. Since we're 0.x, now is the time to standardize before more messages accumulate.

## What Changes

- **BREAKING** Rename commands across all domains to follow a unified convention
- **BREAKING** Rename query messages to follow a unified convention
- **BREAKING** Rename response messages to follow a unified convention
- Add missing `IDownloadResponse` marker interface and apply it to all Download response types
- Document the naming convention as a spec for future messages

## Capabilities

### New Capabilities
- `message-naming-standards`: Formal naming convention for all message types in FunkArr.Messages — commands, queries, responses, data carriers, and marker interfaces

### Modified Capabilities
- `message-response-interfaces`: Add `IDownloadResponse` marker interface for the Download domain
- `download-messages`: Rename messages to follow the unified convention
- `search-messages`: Rename messages to follow the unified convention (Search already mostly conforms, minor adjustments)

## Impact

- **FunkArr.Messages**: All message record renames happen here
- **FunkArr.Search**: Update all references to renamed search messages
- **FunkArr.Download**: Update all references to renamed download messages
- **FunkArr.RuleSet**: Update all references to renamed ruleset/scoring messages
- **FunkArr.MatchMagic**: Update all references to renamed scoring messages
- **FunkArr.MetadataResolver**: Update all references to renamed resolution messages
- **FunkArr.Api**: Update all Ask/Receive/handler references
- **FunkArr.ArrApi**: Update all Ask/Receive/handler references
- **FunkArr.Persistence**: No changes (persistence DTOs use past-tense events, unaffected)
- **Tests**: Update all test references to renamed messages
- **No external API changes** — Newznab/SABnzbd APIs are unaffected
