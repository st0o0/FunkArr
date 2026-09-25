# network-routing Specification

## Purpose
TBD - created by archiving change network-routing-and-proxy. Update Purpose after archive.
## Requirements
### Requirement: RoutingOptions configuration model
The system SHALL provide a `RoutingOptions` configuration class bound to the `FunkArr:Routes` section, containing a list of `RouteDefinition` entries, a list of `ChannelRoute` pattern mappings, and a `Default` route name.

#### Scenario: Default configuration
- **WHEN** no `FunkArr:Routes` section is configured
- **THEN** the system SHALL use a single implicit `Direct` route with no proxy
- **AND** the default route SHALL be `Direct`

#### Scenario: Full configuration
- **WHEN** the `FunkArr:Routes` section defines route definitions, channel routes, and a default
- **THEN** the system SHALL bind them to `RoutingOptions` with `Definitions`, `ChannelRoutes`, and `Default` properties

### Requirement: RouteDefinition model
A `RouteDefinition` SHALL have a `Name` (string) and an optional `Proxy` (string, URI). When `Proxy` is null or empty, the route represents a direct connection with no proxy.

#### Scenario: Direct route
- **WHEN** a RouteDefinition has Name "Direct" and no Proxy
- **THEN** it SHALL represent a connection without proxy

#### Scenario: Proxy route
- **WHEN** a RouteDefinition has Name "Austria" and Proxy "http://tinyproxy-at:8888"
- **THEN** it SHALL represent a connection through the specified HTTP proxy

### Requirement: ChannelRoute model
A `ChannelRoute` SHALL have a `Pattern` (string, glob expression) and a `Route` (string, referencing a RouteDefinition name).

#### Scenario: Pattern structure
- **WHEN** a ChannelRoute is defined with Pattern "ORF*" and Route "Austria"
- **THEN** it SHALL map any channel matching the glob pattern to the named route

### Requirement: Route resolver resolves channel to route
The system SHALL provide an `IRouteResolver` service that accepts a channel string and returns a `ResolvedRoute` containing the route name and optional proxy URI. Pattern matching SHALL use `FileSystemName.MatchesSimpleExpression` with first-match-wins semantics.

#### Scenario: Channel matches a pattern
- **WHEN** the resolver receives channel "ORF" and a ChannelRoute with pattern "ORF*" exists
- **THEN** the resolver SHALL return the route definition referenced by that ChannelRoute

#### Scenario: Channel matches multiple patterns
- **WHEN** the resolver receives a channel that matches more than one ChannelRoute pattern
- **THEN** the resolver SHALL return the route from the first matching ChannelRoute in list order

#### Scenario: No pattern matches
- **WHEN** the resolver receives channel "ZDF" and no ChannelRoute pattern matches
- **THEN** the resolver SHALL return the route definition referenced by the Default setting

#### Scenario: Direct route resolved
- **WHEN** the resolved route definition has no Proxy configured
- **THEN** the ResolvedRoute SHALL have a null ProxyUrl

### Requirement: Configuration validation on startup
The system SHALL validate `RoutingOptions` on startup using `IValidateOptions<RoutingOptions>` and fail fast on invalid configuration.

#### Scenario: ChannelRoute references undefined route
- **WHEN** a ChannelRoute references a Route name that does not exist in Definitions
- **THEN** validation SHALL fail with an error identifying the invalid reference

#### Scenario: Default references undefined route
- **WHEN** the Default value references a route name that does not exist in Definitions
- **THEN** validation SHALL fail with an error identifying the invalid default

#### Scenario: Duplicate route names
- **WHEN** two or more RouteDefinitions share the same Name
- **THEN** validation SHALL fail with an error identifying the duplicates

#### Scenario: Invalid proxy URI
- **WHEN** a RouteDefinition has a Proxy value that is not a valid absolute HTTP or HTTPS URI
- **THEN** validation SHALL fail with an error identifying the invalid URI

#### Scenario: Valid configuration
- **WHEN** all ChannelRoute and Default references resolve to defined routes, names are unique, and proxy URIs are valid
- **THEN** validation SHALL succeed

### Requirement: Environment variable convention
Route configuration SHALL follow the existing `FunkArr__` double-underscore convention for nested configuration via environment variables.

#### Scenario: Route definitions via env vars
- **WHEN** environment variables `FunkArr__Routes__Definitions__0__Name=Austria` and `FunkArr__Routes__Definitions__0__Proxy=http://proxy:8888` are set
- **THEN** the system SHALL bind them as the first RouteDefinition

#### Scenario: Channel routes via env vars
- **WHEN** environment variables `FunkArr__Routes__ChannelRoutes__0__Pattern=ORF*` and `FunkArr__Routes__ChannelRoutes__0__Route=Austria` are set
- **THEN** the system SHALL bind them as the first ChannelRoute

#### Scenario: Default via env var
- **WHEN** environment variable `FunkArr__Routes__Default=Direct` is set
- **THEN** the system SHALL use "Direct" as the default route name

