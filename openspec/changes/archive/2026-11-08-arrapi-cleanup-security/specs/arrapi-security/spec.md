## ADDED Requirements

### Requirement: ForwardedHeaders middleware SHALL be registered

The application SHALL register `ForwardedHeadersMiddleware` early in the middleware pipeline (before routing and authentication) to support correct `Request.Scheme` and `Request.Host` values when running behind a TLS-terminating reverse proxy.

#### Scenario: Correct scheme behind TLS proxy

- **WHEN** a request arrives with `X-Forwarded-Proto: https` and `X-Forwarded-Host: funkarr.example.com`
- **THEN** `Request.Scheme` SHALL be `https` and `Request.Host` SHALL be `funkarr.example.com`

#### Scenario: Direct access without proxy headers

- **WHEN** a request arrives without `X-Forwarded-*` headers
- **THEN** `Request.Scheme` and `Request.Host` SHALL reflect the actual connection values

#### Scenario: Docker network compatibility

- **WHEN** FunkArr runs behind a reverse proxy in a Docker network
- **THEN** ForwardedHeaders SHALL accept proxy headers from any source address (KnownProxies and KnownNetworks cleared)

### Requirement: ForwardedHeaders SHALL forward proto and host

The `ForwardedHeadersOptions` SHALL be configured with `ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto`. `KnownProxies` and `KnownNetworks` SHALL be cleared to allow any source.

#### Scenario: Configuration values

- **WHEN** ForwardedHeaders middleware is registered
- **THEN** the options SHALL have `XForwardedFor` and `XForwardedProto` enabled and `KnownProxies`/`KnownNetworks` empty

### Requirement: Timing-safe API key comparison

`ApiKeyActionFilter` SHALL compare the provided API key against the expected key using `CryptographicOperations.FixedTimeEquals` on the UTF-8 byte representation, preventing timing side-channel attacks.

#### Scenario: Valid key accepted

- **WHEN** a request includes `apikey=correct-key` and the expected key is `correct-key`
- **THEN** the request SHALL proceed to the controller action

#### Scenario: Invalid key rejected

- **WHEN** a request includes `apikey=wrong-key`
- **THEN** the filter SHALL return the protocol-appropriate error response (Newznab XML or SABnzbd JSON)

#### Scenario: Comparison is constant-time

- **WHEN** the API key is compared
- **THEN** the comparison SHALL use `CryptographicOperations.FixedTimeEquals` (not string equality)

#### Scenario: Missing key rejected

- **WHEN** a request omits the `apikey` parameter
- **THEN** the filter SHALL return the protocol-appropriate error response
