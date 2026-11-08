# newznab-search-decomposition Specification

## Purpose

Decomposition of `NewznabSearchService` into focused classes: RSS item building (`RssItemBuilder`), Newznab attribute assembly (`NewznabAttributeBuilder`), and search orchestration (slimmed service).

## Requirements

### Requirement: RSS item construction SHALL be in RssItemBuilder

A sealed `RssItemBuilder` class in `FunkArr.ArrApi.Newznab.Services` SHALL be responsible for constructing `Item` POCOs from `SearchResultItem` domain results and assembling complete `Rss` channel documents. It SHALL be registered as Scoped in DI.

#### Scenario: Single item construction

- **WHEN** `RssItemBuilder.ToItem(searchResultItem, baseUrl, apiKey)` is called
- **THEN** it SHALL return an `Item` POCO with title, guid, link, pubDate, enclosure, description, comments, category, and newznab attributes populated from the search result

#### Scenario: RSS document assembly

- **WHEN** `RssItemBuilder.CreateRss(items, offset, total)` is called
- **THEN** it SHALL return an `Rss` POCO with a channel containing the provided items and a `newznab:response` element with the given offset and total

#### Scenario: Base URL used for enclosure and link

- **WHEN** an item is constructed with baseUrl `https://funkarr.local`
- **THEN** the enclosure URL and link SHALL use `https://funkarr.local` as prefix

### Requirement: Newznab attribute assembly SHALL be in NewznabAttributeBuilder

A sealed `NewznabAttributeBuilder` class in `FunkArr.ArrApi.Newznab.Services` SHALL be responsible for assembling `List<NewznabAttribute>` from a `SearchResultItem` and its category. It SHALL be registered as Scoped in DI.

#### Scenario: Size attribute

- **WHEN** a search result has `EstimatedSize = 524288000`
- **THEN** the attributes SHALL include `name="size" value="524288000"`

#### Scenario: Category attribute

- **WHEN** a search result is in the TV HD category
- **THEN** the attributes SHALL include `name="category" value="5040"`

#### Scenario: Media ID attributes

- **WHEN** a search result has TvdbId=83214, ImdbId="tt0806910", TmdbId=2116
- **THEN** the attributes SHALL include `tvdbid`, `imdb`, and `tmdbid` attributes with those values

#### Scenario: Missing media IDs omitted

- **WHEN** a search result has no media IDs (all null)
- **THEN** no media ID attributes SHALL be emitted

#### Scenario: Season and episode attributes

- **WHEN** a search result has Season="2026" and Episode="17"
- **THEN** the attributes SHALL include `name="season" value="2026"` and `name="episode" value="17"`

### Requirement: NewznabSearchService SHALL delegate building to extracted classes

`NewznabSearchService` SHALL use `RssItemBuilder` for RSS construction and `NewznabAttributeBuilder` (via `RssItemBuilder`) for attribute assembly. The service SHALL retain search orchestration (cache, actor Ask), command building, filtering, and pagination.

#### Scenario: Service uses RssItemBuilder for search results

- **WHEN** a search returns results from the actor
- **THEN** `NewznabSearchService` SHALL delegate RSS construction to `RssItemBuilder`

#### Scenario: Service uses RssItemBuilder for feed results

- **WHEN** a feed-mode request returns entries
- **THEN** `NewznabSearchService` SHALL delegate RSS construction to `RssItemBuilder`

#### Scenario: Service retains search orchestration

- **WHEN** a search request arrives
- **THEN** `NewznabSearchService` SHALL still handle cache lookup, actor Ask, error handling, and result coordination

#### Scenario: Service retains command building

- **WHEN** a TV/movie/general search request arrives
- **THEN** `NewznabSearchService` SHALL still build the `SearchCommand` from the `IndexerRequest`

### Requirement: Extracted classes SHALL be registered in DI

`RssItemBuilder` and `NewznabAttributeBuilder` SHALL be registered as Scoped services in `ServiceCollectionExtensions.AddArrApiServices()`.

#### Scenario: DI registration

- **WHEN** `services.AddArrApiServices(configuration)` is called
- **THEN** `RssItemBuilder` and `NewznabAttributeBuilder` SHALL be resolvable from the service provider
