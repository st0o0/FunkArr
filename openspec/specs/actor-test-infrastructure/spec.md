## ADDED Requirements

### Requirement: Actor tests use Akka.Hosting.TestKit base class

All test classes that test actor behavior SHALL extend
`Akka.Hosting.TestKit.TestKit` and use its lifecycle management instead of manually
creating and disposing `ActorSystem` instances.

#### Scenario: Actor test class inherits TestKit

- **WHEN** an actor test class is defined
- **THEN** it extends `Akka.Hosting.TestKit.TestKit` and overrides `ConfigureAkka`
  to register actors and test probes

#### Scenario: TestKit manages actor system lifecycle

- **WHEN** a test executes
- **THEN** the `ActorSystem` is created and disposed by the TestKit base class, not
  by manual `ActorSystem.Create` / `Terminate` calls

### Requirement: Test probes for actor dependency verification

Actor tests SHALL use `CreateTestProbe()` to create test probes that stand in for
dependency actors, enabling message assertion via `ExpectMsgAsync<T>()`.

#### Scenario: Test probe receives forwarded message

- **WHEN** an actor under test sends a message to a dependency
- **THEN** a test probe registered as that dependency receives the message and can
  assert on its contents via `ExpectMsgAsync<T>()`

### Requirement: ActorRegistry used for actor resolution in tests

Tests SHALL register actors and test probes via `ActorRegistry.Register<T>()` in
`ConfigureAkka` and resolve them via `ActorRegistry.Get<T>()` for assertions.

#### Scenario: Test registers probe as dependency

- **WHEN** a test sets up a dependency actor
- **THEN** it registers the test probe via `registry.Register<T>(probe)` so the
  actor under test can resolve it via `GetActorAsync<T>()`

### Requirement: Shared TestPersistenceConfig helper

A `TestPersistenceConfig` static class in `FunkArr.Tests.Shared` SHALL provide an
`AddTestPersistence` extension method on `AkkaConfigurationBuilder` that configures
in-memory journal and snapshot store.

#### Scenario: In-memory persistence in tests

- **WHEN** a test for a persistent actor configures the actor system
- **THEN** it calls `builder.AddTestPersistence()` to use in-memory journal and
  snapshot store instead of SQLite

### Requirement: Tests use timeouts to prevent hanging

All actor test methods SHALL specify a timeout (e.g., `Fact(Timeout = 5000)`) to
prevent hanging tests when an expected message never arrives.

#### Scenario: Test times out on missing message

- **WHEN** an actor test waits for a message that never arrives
- **THEN** the test fails with a timeout after the specified duration instead of
  hanging indefinitely
