## 1. Configuration

- [x] 1.1 Add `TmdbApiKey` property to `SearchOptions`
- [x] 1.2 Add `FunkArr__Search__TmdbApiKey` to `docker-compose.example.yml` and `appsettings.json` comments

## 2. TmdbClient

- [x] 2.1 Create `TmdbClient.cs` with `FindByImdbIdAsync` and `SearchMovieAsync` methods
- [x] 2.2 Create `TmdbMovieInfo` record (Title, OriginalTitle, ReleaseYear, RuntimeMinutes)
- [x] 2.3 Register `TmdbClient` as typed HttpClient in `FunkArrServiceSetup`

## 3. MovieSearchActor Enhancement

- [x] 3.1 Add `TmdbClient` as constructor parameter to `MovieSearchActor`
- [x] 3.2 Implement TMDB resolution flow: IMDB ID lookup → text search fallback → raw query fallback
- [x] 3.3 Populate `MatchContext.ExpectedDurationSeconds` from TMDB runtime
- [x] 3.4 Implement fallback search with original title when German title yields no results
- [x] 3.5 Update `SearchActor.PreStart` to pass `TmdbClient` to `MovieSearchActor` constructor

## 4. Tests

- [x] 4.1 Create `TmdbClientTests` with mocked HttpMessageHandler (IMDB lookup, text search, error cases)
- [x] 4.2 Update `MovieSearchActorTests` to verify TMDB resolution flow and duration filtering
