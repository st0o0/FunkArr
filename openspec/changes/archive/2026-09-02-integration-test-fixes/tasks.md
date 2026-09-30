## 1. Newznab Category Mapping

- [x] 1.1 Populate Caps categories with TV (5000, subcats 5030/5040) and Movies (2000, subcats 2030/2040) in `Caps.cs`
- [x] 1.2 Add a `bool isMovieSearch` parameter to `ToRss` and `BuildAttributes` in `IndexerApiEndpoints.cs`
- [x] 1.3 Pass `isMovieSearch` from endpoint handlers (`HandleTvSearch` → false, `HandleMovieSearch` → true, `HandleGeneralSearch` → based on cat parameter)
- [x] 1.4 Use movie categories (2040/2030) or TV categories (5040/5030) based on `isMovieSearch` in both the `<category>` element and `<newznab:attr name="category">`
- [x] 1.5 Update existing ArrApi tests for category mapping

## 2. Size Estimation

- [x] 2.1 Add size estimation helper method in `MediathekViewWebManager` that calculates size from duration and quality tier when API returns null
- [x] 2.2 Apply estimation at the mapping boundary in `MediathekViewWebManager` where `r.Size ?? 0` is currently used
- [x] 2.3 Add tests for size estimation logic

## 3. Verification

- [x] 3.1 Rebuild Docker image and verify categories show correctly in Prowlarr search results
- [x] 3.2 Verify ORF entries show estimated size instead of 0 B
