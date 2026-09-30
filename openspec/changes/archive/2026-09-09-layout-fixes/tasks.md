## 1. Add route titles

- [x] 1.1 In `main.ts`, add `meta: { title: '...' }` to each route (Dashboard=null, Downloads, History, Setup, RuleSets, etc.)
- [x] 1.2 Add `router.afterEach` hook that sets `document.title` from `to.meta.title`
- [x] 1.3 Build UI to verify
