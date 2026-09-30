## ADDED Requirements

### Requirement: TVDB API key configuration
The `SearchOptions` class SHALL include a `TvdbApiKey` property bound from `FunkArr:Search:TvdbApiKey`.

#### Scenario: TVDB key configured
- **WHEN** the environment variable `FunkArr__Search__TvdbApiKey` is set to "my-tvdb-key"
- **THEN** `SearchOptions.TvdbApiKey` SHALL be "my-tvdb-key"

#### Scenario: TVDB key not configured
- **WHEN** no TVDB key environment variable is set
- **THEN** `SearchOptions.TvdbApiKey` SHALL be null

## MODIFIED Requirements

### Requirement: Search options validation
The `SearchOptionsValidator` SHALL validate that `QualityProbeLimit >= 1`. Missing API keys SHALL produce a log warning but SHALL NOT fail validation. Keys are only required when the generate/preview endpoint is called.

#### Scenario: Valid options without API keys
- **WHEN** `SearchOptions` has no TVDB or TMDB key but `QualityProbeLimit = 5`
- **THEN** validation SHALL pass (keys are optional for basic operation)

#### Scenario: Valid options with API keys
- **WHEN** `SearchOptions` has both keys and valid `QualityProbeLimit`
- **THEN** validation SHALL pass
