namespace FunkArr.Core.Configuration;

public sealed class TmdbOptions
{
    public const string SectionName = "FunkArr:Tmdb";
    public string ApiKey { get; set; } = "";
}
