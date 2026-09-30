## 1. E2E Test Plan updates

- [x] 1.1 Update "Setup Recipe: Generate pagination data" in E2E-TEST-PLAN.md to search 15+ episodes across seasons 2020-2026 instead of 8 from seasons >= 2024
- [x] 1.2 Add "Setup Recipe: Generate history pagination data" in E2E-TEST-PLAN.md with NZB upload flood (10x show + 10x movie, bad URLs, poll until all failed)
- [x] 1.3 Update Test Execution Order in E2E-TEST-PLAN.md: insert step 18.5 "Generate history pagination data" between steps 18 and 19, renumber step 21 to say "15+ searches across seasons 2020-2026"

## 2. E2E Verify Skill updates

- [x] 2.1 Update Phase 3 in skill.md: add step after "Wait for completions + failure" that uploads 20 NZBs (10x cat=show, 10x cat=movie) with 192.0.2.1 URLs, then polls history count until >= 26
- [x] 2.2 Update Phase 4 in skill.md: change "Generate pagination data" from "5+ more Sonarr searches" to "15+ searches across seasons 2020-2026, one per unique season+episode to avoid dedup"
