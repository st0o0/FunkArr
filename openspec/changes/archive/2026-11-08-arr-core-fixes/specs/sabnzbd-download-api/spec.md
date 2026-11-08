## MODIFIED Requirements

### Requirement: MapMediaTypeToCategory returns plural for Movie
`MapMediaTypeToCategory` SHALL return `"movies"` (plural) for `MediaType.Movie`, matching the category name advertised in `get_config.movie_categories` and registered by Radarr during setup.

#### Scenario: Movie category output
- **WHEN** `MapMediaTypeToCategory` is called with `MediaType.Movie`
- **THEN** the result SHALL be `"movies"`

#### Scenario: Show category output unchanged
- **WHEN** `MapMediaTypeToCategory` is called with `MediaType.Show`
- **THEN** the result SHALL be `"tv"`

#### Scenario: Queue slot uses correct category
- **WHEN** a queue item with `MediaType.Movie` is built into a SABnzbd slot
- **THEN** the slot's `Cat` field SHALL be `"movies"`

#### Scenario: History slot uses correct category
- **WHEN** a history item with `MediaType.Movie` is built into a SABnzbd history slot
- **THEN** the slot's `Category` field SHALL be `"movies"`
