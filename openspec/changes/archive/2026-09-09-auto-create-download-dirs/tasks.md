## 1. Auto-create directories

- [x] 1.1 In `DataPaths` constructor, add `Directory.CreateDirectory(Complete)` and `Directory.CreateDirectory(Incomplete)` after path resolution
- [x] 1.2 Use `IFileSystem` if available via DI, or `System.IO.Directory.CreateDirectory` directly since `DataPaths` is constructed in the DI container

## 2. Verify

- [x] 2.1 Run `dotnet build FunkArr.slnx`
- [x] 2.2 Run `dotnet format --verify-no-changes`
