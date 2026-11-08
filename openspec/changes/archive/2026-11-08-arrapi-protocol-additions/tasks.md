## 1. Caps filter params

- [x] 1.1 Add `Title`, `MaxAge`, `MinSize`, `MaxSize` to `IndexerRequest` as optional query params
- [x] 1.2 Update `Caps.cs` supportedParams: search adds `title,maxage,minsize,maxsize`, tv-search and movie-search add same
- [x] 1.3 Add test verifying new caps supportedParams contain all filter params

## 2. Item attributes

- [x] 2.1 Add `language` ("German") attribute — always emitted in `NewznabAttributeBuilder.BuildAttributes`
- [x] 2.2 Add `subs` ("German") attribute — emitted when `item.SubtitleUrl` is not null/empty
- [x] 2.3 Add optional `DateTimeOffset? firstSeenAt` parameter to `BuildAttributes`, emit `usenetdate` when present
- [x] 2.4 Thread `firstSeenAt` through `RssItemBuilder.ToItem` → `BuildAttributes` for feed-backed items
- [x] 2.5 Add tests for language, subs, usenetdate attributes (present and absent cases)

## 3. Error codes

- [x] 3.1 Add error codes 101-104, 203 as static fields on `NewznabError`
- [x] 3.2 Add test verifying all error codes produce valid XML with correct code/description

## 4. Verify

- [x] 4.1 Build passes, format clean, all existing tests pass
