## 1. Dockerfile GC Configuration

- [x] 1.1 Add `DOTNET_gcServer=0` and `DOTNET_GCConserveMemory=9` ENV directives to `Dockerfile`, grouped with the existing `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` line

## 2. Verification

- [x] 2.1 Build the Docker image and verify all three `DOTNET_*` environment variables are present (`docker inspect` or `docker run --rm funkarr env`)
