## REMOVED Requirements

### Requirement: Centralized API key authentication
**Reason**: FunkArr does not need authentication. The API key exists only because Sonarr/Radarr require one as a mandatory field for indexers and download clients. FunkArr runs in a Docker network where all callers are trusted.
**Migration**: Remove `ApiKeyMiddleware` class and its registration in `FunkArrApplicationSetup`. All endpoints become publicly accessible. Frontend removes `apikey` query parameter from requests.

### Requirement: Route-aware error response format
**Reason**: With no authentication middleware, there are no auth error responses to format.
**Migration**: None — the middleware that produced these responses is removed entirely.

### Requirement: Authentication bypass for public routes
**Reason**: With no authentication middleware, bypass logic is unnecessary. All routes are public.
**Migration**: None — the middleware that checked routes is removed entirely.
