# SABnzbd Download API

## MODIFIED Requirements

### Requirement: Config endpoint
The system SHALL respond to `GET /download/api?mode=get_config` with a JSON object containing SABnzbd configuration including complete_dir, categories, sorting settings, and sorters.

#### Scenario: Config response structure
- **WHEN** `?mode=get_config` is requested
- **THEN** the response SHALL be JSON with `config.misc.complete_dir` set to `DownloadOptions.CompletePath` (i.e., `{DownloadPath}/complete`), and `config.categories` dynamically built from `DownloadOptions.Categories`

#### Scenario: Config category entries
- **WHEN** the config is returned and `DownloadOptions.Categories` contains entries
- **THEN** each entry in `config.categories` SHALL contain `name` (from category Name), `order` (index), `dir` (resolved directory name), `newzbin` (empty string), `priority` (0)

#### Scenario: Config with no categories configured
- **WHEN** the config is returned and `DownloadOptions.Categories` is empty
- **THEN** `config.categories` SHALL be an empty array

#### Scenario: Config sorting disabled
- **WHEN** the config is returned
- **THEN** `config.misc.enable_tv_sorting`, `config.misc.enable_movie_sorting`, and `config.misc.enable_date_sorting` SHALL be `false`

#### Scenario: Config pre_check field
- **WHEN** the config is returned
- **THEN** `config.misc.pre_check` SHALL be `false`

#### Scenario: Config history retention
- **WHEN** the config is returned
- **THEN** `config.misc.history_retention` SHALL be `"all"`

#### Scenario: Config sorting category lists
- **WHEN** the config is returned
- **THEN** `config.misc` SHALL contain `tv_categories` (empty array), `movie_categories` (empty array), and `date_categories` (empty array)

#### Scenario: Config sorters empty
- **WHEN** the config is returned
- **THEN** `config.sorters` SHALL be an empty array

### Requirement: Full status endpoint
The system SHALL respond to `GET /download/api?mode=fullstatus` with a JSON status object. This endpoint is called by Sonarr/Radarr during connection testing.

#### Scenario: Full status response structure
- **WHEN** `?mode=fullstatus` is requested
- **THEN** the response SHALL be JSON with a `status` object containing `paused` (bool, default false), `speedlimit` (string, default ""), `diskspace1` (string, free GB), `diskspace2` (string, free GB), `completedir` (string, `DownloadOptions.CompletePath`), and `speed` (string, aggregate bytes/second of active downloads)
