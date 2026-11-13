# Web UI

`src/FunkArr.UI` is a Vue 3 single-page app built with Vite, TypeScript and Tailwind CSS. The Dockerfile builds it and copies `dist/` into the host's `wwwroot`, so FunkArr serves UI and API from the same port.

```
src/FunkArr.UI/
  src/api/            typed clients for /api, enum maps
  src/views/          one component per route (Home, Activity, Feed, RuleSetBuilder, Settings, Setup, …)
  src/components/     shared components
  src/composables/    state and streams (useQueueStream, useLogStream, useRulesetMatcher, …)
  src/i18n/locales/   de, de-AT, de-CH, en
  src/__tests__/      Vitest unit tests
  e2e/                Playwright specs and the manual E2E plan
```

## Running it

Start the backend with the [dev stack](./local-development), then:

```bash
pnpm --dir src/FunkArr.UI dev
```

Vite runs on `http://localhost:5173` with hot reload and proxies `/api`, `/index/api` and `/download/api` to `http://localhost:6969`.

## Things to know

- **Enums arrive as integers.** `src/api/enums.ts` mirrors the API enums and `enumMaps.ts` turns them into labels. When an API enum changes, change both.
- **Live data uses server-sent events.** The download queue and the log view subscribe to `EventSource` streams instead of polling.
- **Every visible string goes through vue-i18n.** Add new keys to `de.json` and `en.json`; `de-AT` and `de-CH` only override what differs.
- **Tests.** `pnpm --dir src/FunkArr.UI test` runs Vitest, `test:ui` runs Playwright against the Vite dev server with the API mocked per test. `e2e/E2E-TEST-PLAN.md` is the manual click-through for release checks with the whole dev stack.
