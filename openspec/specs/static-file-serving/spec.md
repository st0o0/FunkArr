## Purpose

Serve the Vue frontend's built assets as static files from the .NET host, with SPA fallback routing for client-side navigation.

## Requirements

### Requirement: Serve Vue frontend as static files
The .NET host SHALL serve the Vue frontend's built assets from the `FunkArr.UI/dist/` directory as static files. The host SHALL call `app.UseStaticFiles()` in `ApplicationSetupContainer` to enable serving of JS, CSS, SVG, and other static assets.

#### Scenario: Static asset served
- **WHEN** `GET /assets/index-abc123.js` is requested and the file exists in dist/
- **THEN** the response is 200 with the correct content type

#### Scenario: Missing static asset
- **WHEN** `GET /assets/nonexistent.js` is requested
- **THEN** the request falls through to endpoint routing (not a 404 from static files)

### Requirement: SPA fallback routing
The .NET host SHALL configure a fallback route that serves `index.html` for any request that does not match a static file or an API endpoint. This enables client-side routing by vue-router.

#### Scenario: Vue route served
- **WHEN** `GET /rulesets/tatort` is requested (a vue-router route, not a static file)
- **THEN** the response is 200 serving `index.html`, and vue-router handles the route client-side

#### Scenario: API routes not affected
- **WHEN** `GET /api/rulesets` is requested
- **THEN** the API endpoint handles the request, not the SPA fallback

#### Scenario: ArrApi routes not affected
- **WHEN** `GET /index/api?t=caps&apikey=test` is requested
- **THEN** the Newznab endpoint handles the request, not the SPA fallback

### Requirement: Static file middleware ordering
The `ApplicationSetupContainer` SHALL configure middleware in the correct order: `UseStaticFiles()` before endpoint mapping, and the SPA fallback as the last mapped route so it does not interfere with API or ArrApi endpoints.

#### Scenario: Middleware order
- **WHEN** reviewing `ApplicationSetupContainer.SetupApplication`
- **THEN** `UseStaticFiles()` is called before `MapRuleSetApi()`, `MapIndexerApi()`, `MapDownloadApi()`, and the SPA fallback is mapped last
