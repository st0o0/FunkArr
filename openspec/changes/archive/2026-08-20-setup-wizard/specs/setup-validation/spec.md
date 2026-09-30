## ADDED Requirements

### Requirement: Setup validation endpoint authentication
`POST /api/setup/validate` SHALL require the same `apikey` query
parameter authentication as other FunkArr API endpoint groups.

#### Scenario: Valid API key
- **WHEN** a request to `POST /api/setup/validate?apikey=valid-key` is received
- **THEN** the endpoint SHALL execute validation and return a result

#### Scenario: Missing or incorrect API key
- **WHEN** a request to `POST /api/setup/validate` is received without a
  valid `apikey` query parameter
- **THEN** the endpoint SHALL return 401 Unauthorized without executing
  any check

### Requirement: FunkArr API key self-check
The system SHALL validate that FunkArr's own `ApiKey` configuration
value is set to a non-empty value.

#### Scenario: API key configured
- **WHEN** `FunkArrOptions.ApiKey` is a non-empty string
- **THEN** the `api-key` self-check SHALL report status `pass`

#### Scenario: API key missing
- **WHEN** `FunkArrOptions.ApiKey` is empty or unset
- **THEN** the `api-key` self-check SHALL report status `fail` with fix
  guidance describing how to set `FunkArr__ApiKey`

### Requirement: FFmpeg availability self-check
The system SHALL validate that FFmpeg is installed and executable,
reusing the existing `FfmpegHealthCheck` probe logic rather than
duplicating it.

#### Scenario: FFmpeg reachable
- **WHEN** invoking `ffmpeg -version` succeeds with exit code 0
- **THEN** the `ffmpeg` self-check SHALL report status `pass` including
  the detected version in its message

#### Scenario: FFmpeg not found or not executable
- **WHEN** invoking `ffmpeg -version` fails (binary not found, spawn
  error, or non-zero exit code)
- **THEN** the `ffmpeg` self-check SHALL report status `fail` with fix
  guidance describing how to install FFmpeg or fix `PATH`/container
  image configuration

### Requirement: Download path writable self-check
The system SHALL validate that `FunkArrOptions.DownloadPath` exists (or
can be created) and is writable by the process.

#### Scenario: Download path writable
- **WHEN** the configured download path can be created if missing, and a
  test file can be written to and deleted from it
- **THEN** the `download-path` self-check SHALL report status `pass`

#### Scenario: Download path not writable
- **WHEN** the configured download path cannot be created or a test
  file cannot be written to it
- **THEN** the `download-path` self-check SHALL report status `fail`
  with fix guidance describing the required filesystem permissions or
  volume mount

### Requirement: Temp path writable self-check
The system SHALL validate that `FunkArrOptions.TempPath` exists (or can
be created) and is writable by the process, independently of the
download path check.

#### Scenario: Temp path writable
- **WHEN** the configured temp path can be created if missing, and a
  test file can be written to and deleted from it
- **THEN** the `temp-path` self-check SHALL report status `pass`

#### Scenario: Temp path not writable
- **WHEN** the configured temp path cannot be created or a test file
  cannot be written to it
- **THEN** the `temp-path` self-check SHALL report status `fail` with
  fix guidance describing the required filesystem permissions or volume
  mount

### Requirement: Prowlarr connectivity and registration check
Given a Prowlarr URL and API key in the validation request, the system
SHALL check whether Prowlarr is reachable and, if reachable, whether
FunkArr is registered as an indexer.

#### Scenario: Prowlarr connection details omitted
- **WHEN** the validation request does not include a `prowlarr` section
- **THEN** no Prowlarr checks SHALL be included in the result

#### Scenario: Prowlarr unreachable
- **WHEN** a `prowlarr` section is supplied but the request to
  Prowlarr's health/status endpoint fails (network error, timeout, or
  non-2xx response)
- **THEN** the `prowlarr-connectivity` check SHALL report status `fail`
  with fix guidance to verify the URL and API key, and the
  `prowlarr-registered` check SHALL report status `fail` with message
  indicating it was skipped because Prowlarr was unreachable

#### Scenario: Prowlarr reachable, FunkArr registered
- **WHEN** Prowlarr is reachable and its indexer list
  (`GET /api/v1/indexer`) contains an entry that matches FunkArr by name
  and, when `selfUrl` was supplied, by host/port
- **THEN** the `prowlarr-connectivity` check SHALL report status `pass`
  and the `prowlarr-registered` check SHALL report status `pass`

#### Scenario: Prowlarr reachable, FunkArr not registered
- **WHEN** Prowlarr is reachable but no indexer entry matches FunkArr by
  name or host
- **THEN** the `prowlarr-connectivity` check SHALL report status `pass`
  and the `prowlarr-registered` check SHALL report status `warning` with
  fix guidance describing how to add FunkArr as a Newznab indexer in
  Prowlarr

#### Scenario: Prowlarr reachable, ambiguous registration match
- **WHEN** Prowlarr is reachable and an indexer entry matches FunkArr by
  name but its host/port does not match the supplied `selfUrl` (or
  `selfUrl` was not supplied)
- **THEN** the `prowlarr-registered` check SHALL report status `warning`
  indicating the match could not be confirmed

### Requirement: Sonarr/Radarr connectivity and registration check
Given one or more Arr instance connections (Sonarr or Radarr, each with
URL and API key) in the validation request, the system SHALL check
whether each instance is reachable and, if reachable, whether FunkArr is
registered as a download client.

#### Scenario: No Arr instances supplied
- **WHEN** the validation request's `arrInstances` list is empty or
  omitted
- **THEN** no Sonarr/Radarr checks SHALL be included in the result

#### Scenario: Arr instance unreachable
- **WHEN** an Arr instance's connection details are supplied but the
  request to its system-status endpoint fails (network error, timeout,
  or non-2xx response)
- **THEN** the `{instance}-connectivity` check SHALL report status
  `fail` with fix guidance to verify the URL and API key, and the
  `{instance}-registered` check for that instance SHALL report status
  `fail` with message indicating it was skipped

#### Scenario: Arr instance reachable, FunkArr registered
- **WHEN** an Arr instance is reachable and its download client list
  (`GET /api/v3/downloadclient`) contains an entry that matches FunkArr
  by name and, when `selfUrl` was supplied, by host/port
- **THEN** the `{instance}-connectivity` check SHALL report status
  `pass` and the `{instance}-registered` check SHALL report status
  `pass`

#### Scenario: Arr instance reachable, FunkArr not registered
- **WHEN** an Arr instance is reachable but no download client entry
  matches FunkArr by name or host
- **THEN** the `{instance}-connectivity` check SHALL report status
  `pass` and the `{instance}-registered` check SHALL report status
  `warning` with fix guidance describing how to add FunkArr as a SABnzbd
  download client in that Arr app

#### Scenario: Multiple Arr instances validated independently
- **WHEN** the validation request includes multiple `arrInstances`
  entries (e.g. one Sonarr, one Radarr)
- **THEN** the system SHALL produce a distinct set of checks per
  instance, and a failure or warning on one instance SHALL NOT affect
  the checks reported for another instance

### Requirement: Structured check result
Every check performed by the setup validation endpoint, self or
external, SHALL return the same result structure: a category, a name, a
status of `pass`, `warning`, or `fail`, a human-readable message
describing what was observed, and fix guidance.

#### Scenario: Passing check has no fix guidance
- **WHEN** a check's status is `pass`
- **THEN** its fix guidance field SHALL be null or omitted

#### Scenario: Non-passing check includes fix guidance
- **WHEN** a check's status is `warning` or `fail`
- **THEN** its fix guidance field SHALL be a non-empty, actionable
  description of the concrete next step to resolve it

#### Scenario: Overall status derived from individual checks
- **WHEN** the validation endpoint returns its aggregate result
- **THEN** the response SHALL include an overall status that is `fail`
  if any individual check is `fail`, otherwise `warning` if any
  individual check is `warning`, otherwise `pass`

### Requirement: No side effects on external systems
Setup validation SHALL NOT create, modify, or delete any resource in
Prowlarr, Sonarr, Radarr, or any other external system. It SHALL only
issue read-only requests (e.g. status/health/list endpoints).

#### Scenario: Validation issues only read requests
- **WHEN** the setup validation endpoint runs any external check
- **THEN** it SHALL only send HTTP `GET` (or equivalent read-only)
  requests to Prowlarr/Sonarr/Radarr, and SHALL NOT send `POST`, `PUT`,
  `PATCH`, or `DELETE` requests to those systems

#### Scenario: Supplied credentials are not persisted
- **WHEN** a validation request includes Prowlarr or Arr instance API
  keys
- **THEN** the system SHALL use them only for the duration of the
  request and SHALL NOT write them to `FunkArrOptions`, configuration
  files, or any persistent store

### Requirement: Partial results on individual check failure
If any single check throws an unexpected error or cannot complete, the
system SHALL still execute and report the results of all other checks
rather than aborting the entire validation request.

#### Scenario: One check throws, others still run
- **WHEN** one check's implementation throws an unhandled exception
  (e.g. an unexpected response shape from an Arr app)
- **THEN** that check SHALL be reported with status `fail` and a message
  summarizing the error, and every other requested check SHALL still
  execute and be included in the response

#### Scenario: Response always returned
- **WHEN** the validation endpoint is called with a well-formed request
  body
- **THEN** the endpoint SHALL return a 200 response containing results
  for every requested check, never a 5xx response caused by an
  individual check's failure
