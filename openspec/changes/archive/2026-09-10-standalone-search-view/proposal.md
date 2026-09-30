## Why

The Mediathek search capability is buried inside the RuleSet Builder's debugger panel, only accessible when editing a ruleset. Users have no way to browse what's available in the Mediatheken or explore content independently. The backend already supports rich search via MediathekViewWeb with fields like duration, quality variants, timestamps, and descriptions - but the internal API strips most of this data down to a minimal `MediathekSearchResult` model. A standalone search view makes this powerful capability directly accessible.

## What Changes

- Add a new `/search` route with a dedicated Search view
- Extend the mediathek search API to return richer result data (size, aired date, description, subtitle availability, quality variants)
- Search input with debounced query, channel/duration filters
- Result cards showing rich metadata: title, topic, channel, duration, quality, aired date, size, description
- Server-side pagination using the existing `Offset`/`Size` parameters on `QueryMediathek`
- Total result count display (from `MediathekQueryCompleted.Total`)

## Capabilities

### New Capabilities
- `search-browse-ui`: Standalone search/browse view for exploring Mediathek content with rich result cards, filters, and pagination

### Modified Capabilities
- `mediathek-gateway`: Extend the internal API search response model to expose additional fields from `MediathekItem` (size, all URL variants, website URL)

## Impact

- **Api**: Extended `MediathekSearchResult` model with additional fields, pagination support via offset/limit query params, total count in response
- **UI**: New Search view, new route in router, new sidebar navigation entry under "Media" group
- **No domain changes** - the MediathekViewWebManager and QueryMediathek message already support all needed features
- **No breaking changes** - the existing endpoint gains new response fields (additive)
