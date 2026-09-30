## 1. Add catch-all route

- [x] 1.1 In `main.ts`, add `{ path: '/:pathMatch(.*)*', redirect: '/' }` as the last route in the routes array

## 2. Verify

- [x] 2.1 Build the UI (`npm run build` in FunkArr.UI)
- [x] 2.2 Navigate to `/nonexistent` and verify it redirects to Dashboard
