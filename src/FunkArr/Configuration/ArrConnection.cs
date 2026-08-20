namespace FunkArr.Configuration;

public sealed class ArrConnection
{
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class ArrInstanceConnection
{
    public string Name { get; set; } = string.Empty;
    public ArrType Type { get; set; } = ArrType.Sonarr;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public enum ArrType
{
    Sonarr,
    Radarr,
}
