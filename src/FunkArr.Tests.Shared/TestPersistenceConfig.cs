using Akka.Hosting;
using Akka.Persistence.Hosting;

namespace FunkArr.Tests.Shared;

public static class TestPersistenceConfig
{
    public static AkkaConfigurationBuilder AddTestPersistence(this AkkaConfigurationBuilder builder)
        => builder.WithInMemoryJournal().WithInMemorySnapshotStore();
}
