## ADDED Requirements

### Requirement: QueryOperator enum

`QueryOperator` SHALL be an enum in namespace `FunkArr.Search` with values: `And`, `Or`. It controls the `operator` field on individual query items sent to the MediathekViewWeb API.

#### Scenario: Enum values
- **WHEN** `QueryOperator` is referenced
- **THEN** it SHALL have exactly two values: `And` and `Or`

### Requirement: Search entry point

`MediathekSearchQuery` SHALL expose a static factory method `Search(string query, QueryOperator op = QueryOperator.And)` that returns a `QueryBuilder` with the search term targeting `["topic", "title", "description"]` and the specified operator.

#### Scenario: Search entry point default operator
- **WHEN** `MediathekSearchQuery.Search("Krimi")` is called
- **THEN** it SHALL return a `QueryBuilder` that builds a query searching `["topic", "title", "description"]` with operator `And`

#### Scenario: Search entry point with Or operator
- **WHEN** `MediathekSearchQuery.Search("Tatort Münster", QueryOperator.Or)` is called
- **THEN** it SHALL return a `QueryBuilder` that builds a query searching `["topic", "title", "description"]` with operator `Or`

### Requirement: Duration filter

The `QueryBuilder` SHALL expose `WithDuration(int? min = null, int? max = null)` that sets duration range constraints. At least one of `min` or `max` MUST be provided. Both are in seconds.

#### Scenario: Duration min only
- **WHEN** `.WithDuration(min: 2400).Build()` is called
- **THEN** the query SHALL have `DurationMin = 2400` and `DurationMax = null`

#### Scenario: Duration range
- **WHEN** `.WithDuration(min: 600, max: 7200).Build()` is called
- **THEN** the query SHALL have `DurationMin = 600` and `DurationMax = 7200`

#### Scenario: Duration validation
- **WHEN** `.WithDuration().Build()` is called with both `min` and `max` null
- **THEN** `Build()` SHALL throw `ArgumentException`

### Requirement: Future exclusion

The `QueryBuilder` SHALL expose `ExcludeFuture()` that marks the query to exclude items with timestamps in the future. Default is to include future items (no `future` field sent).

#### Scenario: ExcludeFuture set
- **WHEN** `.ExcludeFuture().Build()` is called
- **THEN** the query SHALL have `ExcludeFuture = true`

#### Scenario: Default includes future
- **WHEN** no `ExcludeFuture()` is called
- **THEN** the query SHALL have `ExcludeFuture = false`

### Requirement: Wire format operator field

When translating `MediathekSearchQuery` to wire format, each `MediathekQueryItem` SHALL include an `Operator` field (`"and"` or `"or"`). Entry-point query items (`Topic`, `Search`) use the operator from the builder. `WithTitle` and `FromChannel` query items SHALL always use `"and"`.

#### Scenario: Topic with And operator
- **WHEN** a query built via `ByTopic("Tatort", QueryOperator.And)` is translated
- **THEN** the wire format SHALL have `{ fields: ["topic"], query: "Tatort", operator: "and" }`

#### Scenario: Search with Or operator
- **WHEN** a query built via `Search("Tatort Münster", QueryOperator.Or)` is translated
- **THEN** the wire format SHALL have `{ fields: ["topic", "title", "description"], query: "Tatort Münster", operator: "or" }`

#### Scenario: FromChannel always And
- **WHEN** a query built via `Search("Tatort", QueryOperator.Or).FromChannel("ARD")` is translated
- **THEN** the channel query item SHALL have `operator: "and"` regardless of the entry-point operator

### Requirement: Wire format duration fields

When `DurationMin` or `DurationMax` is set on the query, the wire format `MediathekQuery` SHALL include `duration_min` and/or `duration_max` as top-level integer fields. Null values SHALL be omitted from serialization.

#### Scenario: Duration min in wire format
- **WHEN** a query with `DurationMin = 2400` is translated
- **THEN** the wire JSON SHALL include `"duration_min": 2400` and omit `"duration_max"`

#### Scenario: Duration range in wire format
- **WHEN** a query with `DurationMin = 600` and `DurationMax = 7200` is translated
- **THEN** the wire JSON SHALL include `"duration_min": 600` and `"duration_max": 7200`

### Requirement: Wire format future field

When `ExcludeFuture` is true on the query, the wire format `MediathekQuery` SHALL include `"future": false`. When `ExcludeFuture` is false (default), the `future` field SHALL be omitted from serialization.

#### Scenario: ExcludeFuture in wire format
- **WHEN** a query with `ExcludeFuture = true` is translated
- **THEN** the wire JSON SHALL include `"future": false`

#### Scenario: Default omits future
- **WHEN** a query with `ExcludeFuture = false` is translated
- **THEN** the wire JSON SHALL NOT include a `"future"` field

## MODIFIED Requirements

### Requirement: MediathekSearchQuery immutable record

`MediathekSearchQuery` SHALL be a sealed record in namespace `FunkArr.Search` with the following properties: `Topic` (string?), `Title` (string?), `Channel` (string?), `Search` (string?), `MaxResults` (int, default 200), `SortBy` (SortField, default Timestamp), `SortDir` (SortDirection, default Desc), `Operator` (QueryOperator, default And), `DurationMin` (int?), `DurationMax` (int?), `ExcludeFuture` (bool, default false). The record SHALL be immutable — all properties init-only.

#### Scenario: Record immutability
- **WHEN** a `MediathekSearchQuery` is constructed via the builder
- **THEN** all properties SHALL be init-only and the record SHALL not expose any mutation methods

#### Scenario: Default values
- **WHEN** a `MediathekSearchQuery` is built with only a topic
- **THEN** `MaxResults` SHALL be 200, `SortBy` SHALL be `SortField.Timestamp`, `SortDir` SHALL be `SortDirection.Desc`, `Operator` SHALL be `QueryOperator.And`, `DurationMin` and `DurationMax` SHALL be null, and `ExcludeFuture` SHALL be false

### Requirement: Static factory entry points

`MediathekSearchQuery` SHALL expose static factory methods that return a `QueryBuilder`:
- `ByTopic(string topic, QueryOperator op = QueryOperator.And)` — sets `Topic` field with the given operator
- `Search(string query, QueryOperator op = QueryOperator.And)` — sets `Search` field, searches topic + title + description
- `Latest()` — no search terms, `MaxResults` defaults to 100, for RSS/browse feeds

These SHALL be the only way to begin constructing a query. Direct record construction SHALL not be used by callers.

#### Scenario: ByTopic entry point
- **WHEN** `MediathekSearchQuery.ByTopic("Tatort")` is called
- **THEN** it SHALL return a `QueryBuilder` with `Topic = "Tatort"` and `Operator = And`

#### Scenario: ByTopic with Or operator
- **WHEN** `MediathekSearchQuery.ByTopic("Tatort Münster", QueryOperator.Or)` is called
- **THEN** it SHALL return a `QueryBuilder` with `Topic = "Tatort Münster"` and `Operator = Or`

#### Scenario: Search entry point
- **WHEN** `MediathekSearchQuery.Search("Krimi")` is called
- **THEN** it SHALL return a `QueryBuilder` with `Search = "Krimi"` and `Operator = And`

#### Scenario: Latest entry point
- **WHEN** `MediathekSearchQuery.Latest()` is called
- **THEN** it SHALL return a `QueryBuilder` with no search terms and `MaxResults = 100`

### Requirement: Builder validation on Build

`QueryBuilder.Build()` SHALL validate the query and throw on invalid state:
- `ByTopic` and `Search` MUST NOT both be set (mutual exclusion)
- `MaxResults` MUST be between 1 and 1000 inclusive; values outside this range SHALL throw `ArgumentOutOfRangeException`
- At least one of `Topic`, `Search` MUST be set, unless the query was created via `Latest()`
- `WithDuration` with both `min` and `max` null SHALL throw `ArgumentException`
- If both `DurationMin` and `DurationMax` are set, `DurationMin` MUST be less than or equal to `DurationMax`

#### Scenario: Mutual exclusion violation
- **WHEN** a builder somehow has both `Topic` and `Search` set
- **THEN** `Build()` SHALL throw `ArgumentException`

#### Scenario: Size exceeds API cap
- **WHEN** `.Limit(1001).Build()` is called
- **THEN** `Build()` SHALL throw `ArgumentOutOfRangeException`

#### Scenario: Zero limit
- **WHEN** `.Limit(0).Build()` is called
- **THEN** `Build()` SHALL throw `ArgumentOutOfRangeException`

#### Scenario: Valid Latest query
- **WHEN** `MediathekSearchQuery.Latest().Build()` is called
- **THEN** `Build()` SHALL succeed with no search terms and `MaxResults = 100`

#### Scenario: Duration min exceeds max
- **WHEN** `.WithDuration(min: 7200, max: 600).Build()` is called
- **THEN** `Build()` SHALL throw `ArgumentException`

### Requirement: Wire format translation

The gateway layer SHALL translate `MediathekSearchQuery` to `MediathekQuery` (wire format) using these mapping rules:
- `Topic` → `MediathekQueryItem { Fields = ["topic"], Query = value, Operator = operator }`
- `Title` → `MediathekQueryItem { Fields = ["title"], Query = value, Operator = "and" }`
- `Search` → `MediathekQueryItem { Fields = ["topic", "title", "description"], Query = value, Operator = operator }`
- `Channel` → `MediathekQueryItem { Fields = ["channel"], Query = value, Operator = "and" }`
- `MaxResults` → `MediathekQuery.Size`
- `SortBy` + `SortDir` → `MediathekQuery.SortBy` + `MediathekQuery.SortOrder`
- `DurationMin` → `MediathekQuery.DurationMin`
- `DurationMax` → `MediathekQuery.DurationMax`
- `ExcludeFuture = true` → `MediathekQuery.Future = false`
- `Latest()` (no search terms) → `MediathekQuery.Queries = []`

Multiple non-null fields SHALL produce multiple `MediathekQueryItem` entries (AND semantics).

#### Scenario: Topic-only translation
- **WHEN** a query with `Topic = "Tatort"` and `Operator = And` is translated
- **THEN** the wire format SHALL have one query item: `{ fields: ["topic"], query: "Tatort", operator: "and" }`

#### Scenario: Search translation
- **WHEN** a query with `Search = "Krimi"` and `Operator = And` is translated
- **THEN** the wire format SHALL have one query item: `{ fields: ["topic", "title", "description"], query: "Krimi", operator: "and" }`

#### Scenario: Combined topic + channel translation
- **WHEN** a query with `Topic = "Tatort"` and `Channel = "ARD"` is translated
- **THEN** the wire format SHALL have two query items: topic (with entry-point operator) and channel (with `"and"`)

#### Scenario: Latest translation
- **WHEN** a `Latest()` query is translated
- **THEN** the wire format SHALL have `Queries = []` and `Size = 100`

#### Scenario: Duration and future in translation
- **WHEN** a query with `DurationMin = 2400` and `ExcludeFuture = true` is translated
- **THEN** the wire format SHALL include `duration_min: 2400` and `future: false` at the top level

## REMOVED Requirements

### Requirement: ByFullText entry point (from Static factory entry points)
**Reason**: Replaced by `Search` entry point which searches `["topic", "title", "description"]` instead of `["topic", "title"]`.
**Migration**: Replace all `ByFullText(query)` calls with `Search(query)`.
