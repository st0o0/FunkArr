## 1. Add SanitizeFileName to DataPaths

- [x] 1.1 Add `internal static string SanitizeFileName(string input)` method to `DataPaths` that replaces invalid filename characters (Path.GetInvalidFileNameChars + explicit `/\:*?"<>|` + null byte) with `_`, strips `..` sequences, strips leading/trailing dots and spaces, collapses consecutive underscores, truncates to 200 chars, and returns `"unnamed"` for empty results
- [x] 1.2 Call `SanitizeFileName` in `ResolveDownload` on the `title` parameter before constructing `dirName` and `fileName` (lines 74-76)

## 2. Add path confinement validation

- [x] 2.1 In `ResolveDownload`, after constructing `incompletePath` and `completePath`, resolve with `Path.GetFullPath` and assert they start with `Incomplete` and `Complete` respectively. Throw `InvalidOperationException` on violation.
- [x] 2.2 In `DataFiles.Move`, add confinement check that source and destination are within the download root directories. The `DataPaths` instance is already available via DI. Throw `InvalidOperationException` on violation.

## 3. Tests for SanitizeFileName

- [x] 3.1 Add `SanitizeFileNameTests` in `FunkArr.Download.Tests` (DataPaths is in Core, but download tests already reference Core and test DataPaths): path separator replacement, backslash, colon, null byte, traversal sequences, leading/trailing dots, consecutive underscores collapsed, truncation at 200 chars, empty/all-invalid fallback to "unnamed", null throws ArgumentNullException, clean titles pass through unchanged
- [x] 3.2 Add `ResolveDownloadSanitizationTests` in `FunkArr.Download.Tests`: title with `../` stays confined, title with slashes stays confined, empty title uses fallback, normal titles work unchanged, confinement violation throws

## 4. Verify and format

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 4.3 Run `dotnet run --project src/FunkArr.Download.Tests/FunkArr.Download.Tests.csproj` — all tests pass
