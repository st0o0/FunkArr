namespace FunkArr.Configuration;

public sealed class PostgresOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "funkarr";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

    public string BuildConnectionString() =>
        $"Host={Host};Port={Port};Database={Database};Username={User};Password={Password}";
}
