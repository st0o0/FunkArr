# ruleset-catalog Specification

## Purpose
Community ruleset catalog: schema enforcement for media type fields, generated catalog document, and README discoverability.

## Requirements
### Requirement: Community ruleset schema enforces media
The community ruleset JSON schema SHALL list `media` in the root `required` array. The `mediaReference` definition SHALL list `type` in its `required` array alongside `name`.

#### Scenario: Ruleset without media rejected
- **WHEN** a community ruleset JSON file omits the `media` field
- **THEN** schema validation SHALL fail

#### Scenario: Ruleset without media type rejected
- **WHEN** a community ruleset JSON file has `media` but omits `media.type`
- **THEN** schema validation SHALL fail

#### Scenario: Valid ruleset passes
- **WHEN** a community ruleset JSON file includes `media` with `name` and `type`
- **THEN** schema validation SHALL pass

### Requirement: Community catalog document
A `CATALOG.md` file SHALL exist at `data/community/CATALOG.md`. It SHALL be generated from the community ruleset JSON files and contain two sections: **Shows** and **Movies**, each with an alphabetically sorted Markdown table.

#### Scenario: Catalog table columns
- **WHEN** `CATALOG.md` is generated
- **THEN** each table SHALL contain columns: Name, IMDB, TMDB, Rules

#### Scenario: Catalog grouped by type
- **WHEN** community rulesets include both shows and movies
- **THEN** `CATALOG.md` SHALL list shows under a `## Shows` heading and movies under a `## Movies` heading

#### Scenario: Catalog count summary
- **WHEN** `CATALOG.md` is generated
- **THEN** it SHALL include a summary line stating the total number of community rulesets, shows, and movies

### Requirement: README links to catalog
The root `README.md` SHALL contain a link to `data/community/CATALOG.md` so users can browse supported content from the repository landing page.

#### Scenario: README contains catalog link
- **WHEN** a user views the root `README.md`
- **THEN** they SHALL see a link to the community catalog document
