## Why

Prowlarr and the *arr ecosystem expect Newznab capabilities and attributes that FunkArr doesn't yet advertise or emit. Missing caps params (`title`, `maxage`, `minsize`, `maxsize`) mean Prowlarr can't use its built-in filters. Missing item attributes (`language`, `subs`) prevent Arr Custom Formats from matching. Daily shows (Tagesschau, heute journal) break because `ep=11/08` fails `int.TryParse` and silently drops the episode filter. Error codes 101-109/203 are absent from the error catalog.

## What Changes

- **Caps filter params**: Advertise `title`, `maxage`, `minsize`, `maxsize` in supportedParams for all search types. Accept them in `IndexerRequest` (params are parsed but not forwarded to domain — FunkArr doesn't filter by these, but accepting them prevents Prowlarr from reporting unsupported params).
- **Item attributes**: Emit `language` ("German", always), `subs` ("German", when SubtitleUrl present), `usenetdate` (feed first-seen time for feed-backed items) via `NewznabAttributeBuilder`.
- **Daily show ep=MM/DD**: Parse date-format `ep` values alongside integer values so `FilterBySeasonEpisode` can match daily shows.
- **Newznab error codes**: Add codes 101-109 and 203 to `NewznabError` for protocol completeness.

## Capabilities

### New Capabilities
- `newznab-daily-episode`: Date-based episode parameter parsing for daily shows

### Modified Capabilities
- `newznab-indexer-api`: Add filter params to caps, new item attributes, additional error codes

## Impact

- **FunkArr.ArrApi**: `IndexerRequest` gains 4 new optional params, `NewznabAttributeBuilder` emits 3 new attributes, `NewznabError` gains static fields, `Caps` supportedParams updated
- **No domain changes**: Filter params are accepted but not forwarded (FunkArr returns all results, Prowlarr filters client-side)
- **No wire-protocol breaking changes**: Only additions to XML attributes and caps
