## Purpose

Build pipeline for the Vue 3 UI: Vite project structure, build output integration with the .NET host, dev proxy, Tailwind configuration, and Docker multi-stage build.

## Requirements

### Requirement: Vite project structure
The UI project SHALL reside in `src/FunkArr.UI/` with Vue 3, Vite, Tailwind CSS, and TypeScript as its stack.

#### Scenario: Project layout
- **WHEN** the project is set up
- **THEN** `src/FunkArr.UI/` SHALL contain `package.json`, `vite.config.ts`, `tsconfig.json`, `index.html`, and a `src/` directory with Vue components (Tailwind v4 uses CSS-based configuration via the `@tailwindcss/vite` plugin — no `tailwind.config.js` file)

### Requirement: Vite build output to wwwroot
The Vite build SHALL output to `src/FunkArr/wwwroot/`. This directory SHALL be gitignored.

#### Scenario: Build produces static assets
- **WHEN** `npm run build` is executed in `src/FunkArr.UI/`
- **THEN** the output SHALL be written to `src/FunkArr/wwwroot/` containing `index.html` and `assets/` with hashed JS and CSS files

#### Scenario: wwwroot gitignored
- **WHEN** the build output is generated
- **THEN** `src/FunkArr/wwwroot/` SHALL be listed in `.gitignore`

### Requirement: Vite dev proxy
In development mode, the Vite dev server SHALL proxy API requests to the .NET backend.

#### Scenario: Proxy API requests
- **WHEN** the Vite dev server runs on `:5173` and the .NET backend on `:6969`
- **THEN** requests to `/api/*` and `/download/*` SHALL be proxied to `http://localhost:6969`

### Requirement: Tailwind configuration
Tailwind SHALL be configured with system font stack, minimal color palette, and content paths pointing to Vue component files.

#### Scenario: Tailwind purge
- **WHEN** the production build runs
- **THEN** Tailwind SHALL purge unused classes based on content in `src/**/*.vue` and `src/**/*.ts`

### Requirement: Docker build integration
The production Dockerfile expects pre-built UI assets to be present in `src/FunkArr/wwwroot/` before the Docker build. The dev Dockerfile (`Dockerfile.dev`) includes a Node.js build stage that builds the UI during the Docker build. The production Dockerfile does NOT include a Node.js build stage.

#### Scenario: Dev Docker build includes UI stage
- **WHEN** `docker build -f Dockerfile.dev .` is executed
- **THEN** the Node stage SHALL run `npm ci && npm run build` in `src/FunkArr.UI/`, and the .NET stage SHALL copy the output to `src/FunkArr/wwwroot/` before `dotnet publish`

#### Scenario: Production Docker build expects pre-built assets
- **WHEN** the production `docker build` is executed
- **THEN** the build SHALL expect `wwwroot/` assets to already exist (no Node.js build stage)

#### Scenario: Final image has no Node.js
- **WHEN** the Docker image is built
- **THEN** the final stage SHALL be based on the aspnet chiseled image with no Node.js runtime

### Requirement: .NET build independence
The .NET project SHALL build and run without the UI assets present. The UI is optional for development and testing.

#### Scenario: .NET build without wwwroot
- **WHEN** `dotnet build` is executed and `wwwroot/` does not exist
- **THEN** the build SHALL succeed and the application SHALL start (serving API endpoints without the UI)
