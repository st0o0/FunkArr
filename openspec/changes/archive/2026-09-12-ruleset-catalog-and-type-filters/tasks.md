## 1. Schema & Data

- [x] 1.1 Update `data/community/ruleset.schema.json`: add `media` to root `required`, add `type` to `mediaReference.required`
- [x] 1.2 Fix `data/community/rulesets/fernsehfilme-und-serien-serien.json`: add `media` with `name`, `type: "show"`
- [x] 1.3 Fix `data/community/rulesets/unser-sandmaennchen.json`: add `media` with `name`, `type: "show"`
- [x] 1.4 Run schema validation locally to confirm all 71 community rulesets pass

## 2. Backend: MediaType in API

- [x] 2.1 Add `MediaType` field (`string?`) to `RegisteredRuleSetEntry` message in `RuleSetResolverActor`
- [x] 2.2 Populate `MediaType` from the parsed ruleset config in `RuleSetResolverActor` state
- [x] 2.3 Add `mediaType` field to `RuleSetEntry` API model and project it in `RuleSetApiEndpoints`
- [x] 2.4 Build and verify `GET /api/rulesets` returns `mediaType` for movie and show rulesets

## 3. UI: Type Filters & Badge

- [x] 3.1 Update `RuleSetEntry` TypeScript interface in `api/rulesets.ts` to include `mediaType`
- [x] 3.2 Add `typeFilter` state and type filter tabs (All / Shows / Movies with counts) to `RuleSetList.vue`
- [x] 3.3 Add media type badge (`show`/`movie`) to each ruleset card in `RuleSetList.vue`
- [x] 3.4 Wire type filter to compose with existing search filter

## 4. Documentation

- [x] 4.1 Create a PowerShell script to generate `data/community/CATALOG.md` from ruleset JSON files
- [x] 4.2 Run the script to generate the initial `CATALOG.md`
- [x] 4.3 Add a link to the catalog in the root `README.md`
