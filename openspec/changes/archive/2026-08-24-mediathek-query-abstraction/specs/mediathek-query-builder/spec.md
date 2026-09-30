## ADDED Requirements

### Requirement: MediathekSearchQuery immutable record

`MediathekSearchQuery` SHALL be a sealed record in namespace `FunkArr.Search` with the following properties: `Topic` (string?), `Title` (string?), `Channel` (string?), `FullText` (string?), `MaxResults` (int, default 5000), `SortBy` (SortField, default Timestamp), `SortDir` (SortDirection, default Desc). The record SHALL be immutable — all properties init-only.

#### Scenario: Record immutability
- **WHEN** a `MediathekSearchQuery` is constructed via the builder
- **THEN** all properties SHALL be init-only and the record SHALL not expose any mutation methods

#### Scenario: Default values
- **WHEN** a `MediathekSearchQuery` is built with only a topic
- **THEN** `MaxResults` SHALL be 5000, `SortBy` SHALL be `SortField.Timestamp`, and `SortDir` SHALL be `SortDirection.Desc`

### Requirement: Static factory entry points

`MediathekSearchQuery` SHALL expose static factory methods that return a `QueryBuilder`:
- `ByTopic(string topic)` — sets `Topic` field, searches topic only
- `ByFullText(string query)` — sets `FullText` field, searches topic + title
- `Latest()` — no search terms, `MaxResults` defaults to 100, for RSS/browse feeds

These SHALL be the only way to begin constructing a query. Direct record construction SHALL not be used by callers.

#### Scenario: ByTopic entry point
- **WHEN** `MediathekSearchQuery.ByTopic("Tatort")` is called
- **THEN** it SHALL return a `QueryBuilder` with `Topic = "Tatort"` pre-set

#### Scenario: ByFullText entry point
- **WHEN** `MediathekSearchQuery.ByFullText("Krimi")` is called
- **THEN** it SHALL return a `QueryBuilder` with `FullText = "Krimi"` pre-set

#### Scenario: Latest entry point
- **WHEN** `MediathekSearchQuery.Latest()` is called
- **THEN** it SHALL return a `QueryBuilder` with no search terms and `MaxResults = 100`

### Requirement: Builder chainable methods

The `QueryBuilder` SHALL expose chainable methods that return `this`:
- `WithTitle(string title)` — adds a title constraint
- `FromChannel(string channel)` — adds a channel constraint
- `Limit(int maxResults)` — overrides the default max results
- `SortByNewest()` — sets sort to timestamp descending (the default, explicit for readability)

#### Scenario: Chaining WithTitle
- **WHEN** `MediathekSearchQuery.ByTopic("Tatort").WithTitle("Gefangen").Build()` is called
- **THEN** the result SHALL have `Topic = "Tatort"` and `Title = "Gefangen"`

#### Scenario: Chaining FromChannel
- **WHEN** `MediathekSearchQuery.ByTopic("Tatort").FromChannel("ARD").Build()` is called
- **THEN** the result SHALL have `Topic = "Tatort"` and `Channel = "ARD"`

#### Scenario: Chaining Limit
- **WHEN** `MediathekSearchQuery.ByFullText("Krimi").Limit(100).Build()` is called
- **THEN** the result SHALL have `FullText = "Krimi"` and `MaxResults = 100`

### Requirement: Builder validation on Build

`QueryBuilder.Build()` SHALL validate the query and throw `ArgumentException` on invalid state:
- `ByTopic` and `ByFullText` MUST NOT both be set (mutual exclusion)
- `MaxResults` MUST be greater than 0
- At least one of `Topic`, `FullText` MUST be set, unless the query was created via `Latest()`

#### Scenario: Mutual exclusion violation
- **WHEN** a builder somehow has both `Topic` and `FullText` set
- **THEN** `Build()` SHALL throw `ArgumentException`

#### Scenario: Zero limit
- **WHEN** `.Limit(0).Build()` is called
- **THEN** `Build()` SHALL throw `ArgumentException`

#### Scenario: Valid Latest query
- **WHEN** `MediathekSearchQuery.Latest().Build()` is called
- **THEN** `Build()` SHALL succeed with no search terms and `MaxResults = 100`

### Requirement: Wire format translation

The gateway layer SHALL translate `MediathekSearchQuery` to `MediathekQuery` (wire format) using these mapping rules:
- `Topic` → `MediathekQueryItem { Fields = ["topic"], Query = value }`
- `Title` → `MediathekQueryItem { Fields = ["title"], Query = value }`
- `FullText` → `MediathekQueryItem { Fields = ["topic", "title"], Query = value }`
- `Channel` → `MediathekQueryItem { Fields = ["channel"], Query = value }`
- `MaxResults` → `MediathekQuery.Size`
- `SortBy` + `SortDir` → `MediathekQuery.SortBy` + `MediathekQuery.SortOrder`
- `Latest()` (no search terms) → `MediathekQuery.Queries = []`

Multiple non-null fields SHALL produce multiple `MediathekQueryItem` entries (AND semantics).

#### Scenario: Topic-only translation
- **WHEN** a query with `Topic = "Tatort"` is translated
- **THEN** the wire format SHALL have one query item: `{ fields: ["topic"], query: "Tatort" }`

#### Scenario: FullText translation
- **WHEN** a query with `FullText = "Krimi"` is translated
- **THEN** the wire format SHALL have one query item: `{ fields: ["topic", "title"], query: "Krimi" }`

#### Scenario: Combined topic + channel translation
- **WHEN** a query with `Topic = "Tatort"` and `Channel = "ARD"` is translated
- **THEN** the wire format SHALL have two query items: topic and channel (AND combined)

#### Scenario: Latest translation
- **WHEN** a `Latest()` query is translated
- **THEN** the wire format SHALL have `Queries = []` and `Size = 100`

### Requirement: SortField and SortDirection enums

`SortField` SHALL define: `Timestamp`. `SortDirection` SHALL define: `Desc`, `Asc`. These SHALL live in namespace `FunkArr.Search`.

#### Scenario: Default sort values
- **WHEN** no sort methods are called on the builder
- **THEN** `SortBy` SHALL be `SortField.Timestamp` and `SortDir` SHALL be `SortDirection.Desc`
