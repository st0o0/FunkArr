## Purpose

Typed extension methods for `IDistributedCache` providing generic JSON serialization/deserialization, registered as the default cache backend in `ServiceSetupContainer`.

## Requirements

### Requirement: Typed extension methods for IDistributedCache

The `DistributedCacheExtensions` static class in `FunkArr.Core` SHALL provide generic extension methods on `IDistributedCache` for serializing and deserializing cached values using `System.Text.Json`.

#### Scenario: GetAsync with existing entry

- **WHEN** `GetAsync<T>(cache, key, cancellationToken)` is called and the key exists
- **THEN** it SHALL deserialize the stored bytes to `T` using `JsonSerializer.Deserialize<T>` and return the result

#### Scenario: GetAsync with missing entry

- **WHEN** `GetAsync<T>(cache, key, cancellationToken)` is called and the key does not exist
- **THEN** it SHALL return `default(T)`

#### Scenario: SetAsync with absolute expiration

- **WHEN** `SetAsync<T>(cache, key, value, absoluteExpiration, cancellationToken)` is called
- **THEN** it SHALL serialize the value to bytes using `JsonSerializer.SerializeToUtf8Bytes<T>` and store it with the specified `AbsoluteExpirationRelativeToNow`

### Requirement: AddDistributedMemoryCache registration

The `ServiceSetupContainer` in FunkArr.Core SHALL register `services.AddDistributedMemoryCache()` to provide the default in-process `IDistributedCache` implementation.

#### Scenario: Default cache backend

- **WHEN** the application starts without explicit distributed cache configuration
- **THEN** `IDistributedCache` SHALL resolve to the in-process memory-backed implementation
