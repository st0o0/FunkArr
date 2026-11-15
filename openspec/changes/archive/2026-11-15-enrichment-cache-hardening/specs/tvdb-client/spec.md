## MODIFIED Requirements

### Requirement: Fetch episodes by series ID
The TVDB client SHALL expose a method to fetch all episodes for a given TVDB series ID. It SHALL request `GET /series/{id}/episodes/default` with pagination support. The response SHALL be mapped to a list of `TvdbEpisode` records containing: SeasonNumber (int), EpisodeNumber (int), Name (string?), Aired (string?, ISO date), Runtime (int?). The method SHALL track whether all pages completed successfully and report this to the caller via a result type so the cache layer can decide whether to store the result.

#### Scenario: Fetch episodes for a series
- **WHEN** `FetchEpisodes(tvdbId: 83214)` is called
- **THEN** the client SHALL request `/series/83214/episodes/default` and return all episodes across all pages

#### Scenario: Paginated response
- **WHEN** a series has more episodes than one API page returns
- **THEN** the client SHALL follow the `links.next` URL to fetch subsequent pages and combine all episodes

#### Scenario: Series not found
- **WHEN** the TVDB API returns 404 for a series ID
- **THEN** the client SHALL return an empty episode list marked as incomplete

#### Scenario: API error on any page
- **WHEN** the TVDB API returns a 5xx error on any page during pagination
- **THEN** the client SHALL log the error, return whatever episodes were collected so far, and mark the result as incomplete

#### Scenario: Partial success
- **WHEN** pages 0 and 1 succeed but page 2 returns 500
- **THEN** the client SHALL return episodes from pages 0 and 1, marked as incomplete
