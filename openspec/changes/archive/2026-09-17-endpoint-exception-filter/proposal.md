## Why

15 API endpoints across 4 files have identical try/catch boilerplate: inject ILoggerFactory, create logger, catch Exception, log error, return 504. That's 6-8 lines × 15 endpoints = ~100 LOC of copy-paste noise.

## What Changes

- Create `EndpointExceptionFilter` IEndpointFilter in FunkArr.Api
- Register on all API route groups (/api, /index/api, /download/api)
- Remove per-endpoint ILoggerFactory injection and try/catch blocks
- Keep business-logic-specific logging in handlers (e.g. "RuleSet not found")

## Capabilities

### New Capabilities

- `api-exception-filter`: Centralized endpoint exception logging filter

### Modified Capabilities

(none)

## Impact

- FunkArr.Api: new EndpointExceptionFilter.cs, simplified endpoint files
- FunkArr.ArrApi: register filter on Newznab/Sabnzbd route groups
- Net reduction of ~100 LOC boilerplate
