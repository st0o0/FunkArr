namespace FunkArr.IntegrationTests;

[CollectionDefinition("Downloads")]
public sealed class DownloadsCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("RuleSets")]
public sealed class RuleSetsCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("System")]
public sealed class SystemCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("Setup")]
public sealed class SetupCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("Mediathek")]
public sealed class MediathekCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("Newznab")]
public sealed class NewznabCollection : ICollectionFixture<FunkArrFixture>;

[CollectionDefinition("Sabnzbd")]
public sealed class SabnzbdCollection : ICollectionFixture<FunkArrFixture>;
