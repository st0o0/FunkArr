namespace FunkArr.Configuration;

public sealed class FunkArrOptions
{
    public const string SectionName = "FunkArr";

    public string ApiKey { get; set; } = string.Empty;
    public string PersistencePath { get; set; } = "data/funkarr.db";
    public PostgresOptions Postgres { get; set; } = new();
}
