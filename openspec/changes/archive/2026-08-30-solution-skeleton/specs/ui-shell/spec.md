## ADDED Requirements

### Requirement: Vue.js project scaffold
`FunkArr.UI` SHALL be a Vue.js 3 project under `src/FunkArr.UI/` scaffolded with
Vite as the build tool and Tailwind CSS for styling. It SHALL include TypeScript
support and Vue Router.

#### Scenario: UI project builds
- **WHEN** `npm run build` is run from `src/FunkArr.UI/`
- **THEN** the build produces static assets in a `dist/` directory

### Requirement: UI dev server
The UI project SHALL support a dev server via `npm run dev` that proxies API
requests to the FunkArr backend.

#### Scenario: Dev server starts
- **WHEN** `npm run dev` is run from `src/FunkArr.UI/`
- **THEN** a dev server starts on a local port with hot module replacement

### Requirement: UI build integration with host
The host project SHALL serve the built UI assets from `wwwroot/`. The build
pipeline SHALL copy `FunkArr.UI/dist/` contents to `FunkArr/wwwroot/` as part
of the publish step.

#### Scenario: Published host includes UI assets
- **WHEN** the host project is published via `dotnet publish`
- **THEN** the `wwwroot/` directory contains the built Vue.js assets
