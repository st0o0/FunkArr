## 1. Dev Dockerfile

- [x] 1.1 Create `Dockerfile.dev` with SDK build stage (restore with layer caching, then publish)
- [x] 1.2 Add runtime stage with FFmpeg installation and community rulesets copy
- [x] 1.3 Verify `docker build -f Dockerfile.dev .` completes successfully

## 2. Dev Compose

- [x] 2.1 Create `docker-compose.dev.yml` with FunkArr service (build from Dockerfile.dev, port 6969)
- [x] 2.2 Add Sonarr service (home-operations image, port 8989, API key, auth disabled)
- [x] 2.3 Add Radarr service (home-operations image, port 7878, API key, auth disabled)
- [x] 2.4 Add Prowlarr service (home-operations image, port 9696, API key, auth disabled)
- [x] 2.5 Configure shared media volume and per-service config volumes
- [x] 2.6 Set FunkArr environment variables (API key, download path, log format)

## 3. Verification

- [x] 3.1 Run `docker compose -f docker-compose.dev.yml up --build` and verify all services start
- [x] 3.2 Verify FunkArr reachable at localhost:6969, arr services at their respective ports
