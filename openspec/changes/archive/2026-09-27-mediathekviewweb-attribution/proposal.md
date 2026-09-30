## Why

FunkArr depends entirely on MediathekViewWeb for search. Without their free, community-maintained API, FunkArr would not work. Currently MediathekViewWeb is only mentioned as a technical detail in the docs and as a health check label in the UI. Giving them visible credit is the right thing to do and helps users understand what powers their searches.

## What Changes

- Add a feature tile on the docs landing page (`index.md`, both DE and EN) crediting MediathekViewWeb as the data source with a link to `https://mediathekviewweb.de/#everywhere=true`
- Add a `:::tip` callout box in the "How it works" page (`how-it-works.md`, both DE and EN) at the MediathekViewWeb query section explaining the project and linking to it
- Add a "Powered by MediathekViewWeb" link in the UI sidebar footer (`AppLayout.vue`), visible on every page, with i18n strings for all four locales (EN, DE, de-AT, de-CH)

## Capabilities

### New Capabilities

- `mediathekviewweb-attribution`: Visible credit and links to MediathekViewWeb across docs landing page, how-it-works page, and UI sidebar

### Modified Capabilities

- `sidebar-layout`: Add powered-by link to sidebar footer
- `frontend-i18n`: Add attribution i18n strings for all locales
- `docs-i18n`: Add attribution content to both DE and EN doc pages

## Impact

- `docs/index.md` and `docs/en/index.md` - new feature tile
- `docs/how-it-works.md` and `docs/en/how-it-works.md` - new callout box
- `src/FunkArr.UI/src/components/AppLayout.vue` - sidebar footer link
- `src/FunkArr.UI/src/i18n/locales/{en,de,de-AT,de-CH}.json` - new i18n keys
- No backend changes, no API changes, no breaking changes
