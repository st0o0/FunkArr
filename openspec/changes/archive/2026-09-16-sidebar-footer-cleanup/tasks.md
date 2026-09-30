## 1. Remove Version Display

- [ ] 1.1 Remove the version grid block from `AppLayout.vue` sidebar footer (the `v-if="!collapsed && (appVersion || rulesetVersion)"` div). Remove the `appVersion`/`rulesetVersion` refs and the version API call from `onMounted`.

## 2. Verify

- [ ] 2.1 Run `vue-tsc --noEmit` and verify no errors.
