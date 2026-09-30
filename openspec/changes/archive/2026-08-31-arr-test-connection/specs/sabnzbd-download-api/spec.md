## MODIFIED Requirements

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing SABnzbd configuration including complete_dir, categories, sorting settings, and sorters.

#### Scenario: Config response structure
- **WHEN** `?mode=get_config` is requested
- **THEN** the response SHALL be JSON with `config.misc.complete_dir` set to the configured DownloadPath, and `config.categories` containing entries for "sonarr", "radarr", "tv", and "movies"

#### Scenario: Config sorting disabled
- **WHEN** the config is returned
- **THEN** `config.misc.enable_tv_sorting`, `config.misc.enable_movie_sorting`, and `config.misc.enable_date_sorting` SHALL be `false`

#### Scenario: Config sorting category lists
- **WHEN** the config is returned
- **THEN** `config.misc` SHALL contain `tv_categories` (empty array), `movie_categories` (empty array), and `date_categories` (empty array)

#### Scenario: Config sorters empty
- **WHEN** the config is returned
- **THEN** `config.sorters` SHALL be an empty array

## ADDED Requirements

### Requirement: Output query parameter
The system SHALL accept the `output` query parameter on all download API endpoints. The parameter value SHALL be accepted but ignored — the response format is always JSON.

#### Scenario: Output parameter accepted
- **WHEN** `?mode=version&output=json` is requested
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}` (same as without the parameter)

#### Scenario: Output parameter absent
- **WHEN** `?mode=version` is requested without `output`
- **THEN** the response SHALL be JSON `{"version":"4.3.3"}`
