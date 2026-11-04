namespace FunkArr.ArrApi;

public sealed class ArrApiOptions
{
    public const string SectionName = "FunkArr:ArrApi";

    public int SearchCacheTtlSeconds { get; set; } = 60;
}
