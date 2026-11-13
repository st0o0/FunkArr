# Web-Oberfläche

`src/FunkArr.UI` ist eine Vue-3-Single-Page-App mit Vite, TypeScript und Tailwind CSS. Das Dockerfile baut sie und kopiert `dist/` ins `wwwroot` des Hosts; FunkArr liefert Oberfläche und API also über denselben Port aus.

```
src/FunkArr.UI/
  src/api/            typisierte Clients für /api, Enum-Maps
  src/views/          eine Komponente pro Route (Home, Activity, Feed, RuleSetBuilder, Settings, Setup, …)
  src/components/     gemeinsame Komponenten
  src/composables/    Zustand und Streams (useQueueStream, useLogStream, useRulesetMatcher, …)
  src/i18n/locales/   de, de-AT, de-CH, en
  src/__tests__/      Vitest-Unit-Tests
  e2e/                Playwright-Specs und der manuelle E2E-Plan
```

## Starten

Starte das Backend mit dem [Dev-Stack](./local-development), dann:

```bash
pnpm --dir src/FunkArr.UI dev
```

Vite läuft auf `http://localhost:5173` mit Hot Reload und leitet `/api`, `/index/api` und `/download/api` an `http://localhost:6969` weiter.

## Gut zu wissen

- **Enums kommen als Zahlen.** `src/api/enums.ts` spiegelt die API-Enums, `enumMaps.ts` macht Beschriftungen daraus. Ändert sich ein API-Enum, pass beide an.
- **Live-Daten kommen über Server-Sent Events.** Download-Warteschlange und Log-Ansicht abonnieren `EventSource`-Streams, statt zu pollen.
- **Jeder sichtbare Text läuft über vue-i18n.** Neue Schlüssel gehören in `de.json` und `en.json`; `de-AT` und `de-CH` überschreiben nur, was abweicht.
- **Tests.** `pnpm --dir src/FunkArr.UI test` startet Vitest, `test:ui` startet Playwright gegen den Vite-Dev-Server, mit pro Test gemockter API. `e2e/E2E-TEST-PLAN.md` ist der manuelle Durchklick-Plan für Release-Prüfungen mit dem ganzen Dev-Stack.
