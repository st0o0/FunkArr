## ADDED Requirements

### Requirement: ResolveChildActor with Props configuration

The system SHALL provide an `IActorContext.ResolveChildActor<TActor>` extension method overload that accepts a `Func<Props, Props>` parameter to configure the DI-resolved Props before creating the actor. The method SHALL use `DependencyResolver.For(context.System)` to resolve actor dependencies, apply the configure function, then create the child via `context.ActorOf`.

#### Scenario: Create pooled child actor with DI
- **WHEN** an actor calls `Context.ResolveChildActor<MyActor>("name", props => props.WithRouter(new SmallestMailboxPool(2)))`
- **THEN** the system creates a pool of `MyActor` instances where each instance has its constructor dependencies resolved from the DI container

#### Scenario: Create child actor with custom dispatcher
- **WHEN** an actor calls `Context.ResolveChildActor<MyActor>("name", props => props.WithDispatcher("my-dispatcher"))`
- **THEN** the system creates a single `MyActor` child with DI-resolved dependencies running on the specified dispatcher

#### Scenario: Chain multiple Props configurations
- **WHEN** an actor calls `Context.ResolveChildActor<MyActor>("name", props => props.WithRouter(new SmallestMailboxPool(2)).WithDispatcher("my-dispatcher"))`
- **THEN** the system creates a pooled child with both router and dispatcher applied

#### Scenario: Pass extra constructor arguments alongside configure
- **WHEN** an actor calls `Context.ResolveChildActor<MyActor>("name", props => props.WithRouter(...), "extra-arg")`
- **THEN** the system resolves DI dependencies and passes the extra argument to the actor constructor

### Requirement: Extension lives in FunkArr.Core

The `ResolveChildActor` overload SHALL be defined as a static extension method in `FunkArr.Core` under a namespace accessible to all domain projects. It SHALL reference `Akka.DependencyInjection.DependencyResolver` which is transitively available via `Akka.Hosting`.

#### Scenario: Domain project uses the extension
- **WHEN** `FunkArr.MetadataResolver` calls `Context.ResolveChildActor<TvdbResolverActor>("tvdb-pool", props => props.WithRouter(...))`
- **THEN** the call compiles and resolves because FunkArr.Core provides the extension and MetadataResolver references Core
