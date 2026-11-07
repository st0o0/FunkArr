## ADDED Requirements

### Requirement: get_config SHALL include history retention option fields

The SABnzbd `get_config` response `misc` object SHALL include `history_retention_option` with value `"all"` and `history_retention_number` with value `0`, in addition to the existing `history_retention: "all"` field.

#### Scenario: Sonarr evaluates history retention

- **WHEN** Sonarr reads `get_config` from FunkArr
- **THEN** the response SHALL contain `misc.history_retention_option` with value `"all"`
- **THEN** the response SHALL contain `misc.history_retention_number` with value `0`
- **THEN** Sonarr's `DownloadClientRemovesCompletedDownloadsCheck` SHALL not produce a warning

### Requirement: get_config SHALL include category classification arrays

The SABnzbd `get_config` response SHALL include `tv_categories`, `movie_categories`, and `date_categories` arrays populated from configured download categories. Each category whose `MediaType` is `Show` SHALL appear in `tv_categories`. Each category whose `MediaType` is `Movie` SHALL appear in `movie_categories`. `date_categories` SHALL be an empty array.

#### Scenario: Categories populated from configuration

- **WHEN** download categories are configured as `[{name:"tv", mediaType:Show}, {name:"movies", mediaType:Movie}]`
- **THEN** `tv_categories` SHALL be `["tv"]`
- **THEN** `movie_categories` SHALL be `["movies"]`
- **THEN** `date_categories` SHALL be `[]`

#### Scenario: Empty categories when none configured

- **WHEN** no download categories are configured
- **THEN** all three category arrays SHALL be empty

### Requirement: mb and mbleft SHALL use invariant culture formatting

The `mb` and `mbleft` fields on SABnzbd queue and history slots SHALL be formatted with `CultureInfo.InvariantCulture` and at least one decimal place. The formatting SHALL NOT depend on the container's locale settings.

#### Scenario: Invariant decimal formatting

- **WHEN** a queue slot has `TotalBytes` of 1,572,864 (1.5 MB)
- **THEN** `mb` SHALL be `"1.5"` regardless of the server's locale

#### Scenario: Non-English locale does not affect formatting

- **WHEN** the container locale uses comma as decimal separator (e.g., `de-DE`)
- **THEN** `mb` SHALL still use period as decimal separator (e.g., `"1.5"` not `"1,5"`)
