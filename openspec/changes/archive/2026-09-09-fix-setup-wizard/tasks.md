## 1. Fix URL Base values

- [x] 1.1 In `Setup.vue`, change `sonarrConfig()` URL Base from `/download/api` to `/download`
- [x] 1.2 In `Setup.vue`, change `radarrConfig()` URL Base from `/download/api` to `/download`

## 2. Pre-fill host and port from browser location

- [x] 2.1 In `Setup.vue`, replace `<funkarr-host>` placeholder in Prowlarr URL with `window.location.hostname` and port from health check or browser
- [x] 2.2 In `Setup.vue`, replace `<funkarr-host>` and `<funkarr-port>` in Sonarr/Radarr Host/Port fields with actual values from browser location
- [x] 2.3 Mark the pre-filled Host/Port fields as copyable

## 3. Verify

- [x] 3.1 Build the UI (`npm run build` in FunkArr.UI) to verify no errors
- [x] 3.2 Visual check: navigate to Setup, verify all three service configs show actual values
