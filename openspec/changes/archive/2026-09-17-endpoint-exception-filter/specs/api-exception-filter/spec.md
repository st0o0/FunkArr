# api-exception-filter

## Purpose

Centralized endpoint exception logging filter replacing per-endpoint try/catch boilerplate.

## ADDED Requirements

### Requirement: Single endpoint filter for exception logging

An `IEndpointFilter` implementation SHALL catch unhandled exceptions, log them with endpoint route info, and return a gateway timeout response.

#### Scenario: Filter catches unhandled exception
- **WHEN** an endpoint handler throws an unhandled exception
- **THEN** the filter SHALL log it as Error with the endpoint display name and return a 504 response

#### Scenario: No try/catch in endpoint handlers
- **WHEN** any endpoint handler is examined
- **THEN** it SHALL NOT contain a try/catch block whose sole purpose is logging

#### Scenario: Filter registered on all route groups
- **WHEN** the /api, /index/api, and /download/api groups are configured
- **THEN** each SHALL have the exception filter applied

### Requirement: Business-logic logging stays in handlers

Endpoint-specific logging (e.g. "RuleSet not found" at Warning) SHALL remain in handlers. The filter only replaces generic exception catch boilerplate.

#### Scenario: Specific warning stays
- **WHEN** an endpoint logs a domain-specific warning
- **THEN** that log call SHALL remain in the handler
