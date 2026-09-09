namespace FunkArr.Core;

public sealed class PostgresOptions
{
    public const string SectionName = "FunkArr:Postgres";

    public string? Host { get; set; }
    public int Port { get; set; } = 5432;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string Database { get; set; } = "funkarr";

    public string ToConnectionString()
        => $"Host={Host};Port={Port};Username={User};Password={Password};Database={Database}";
}
