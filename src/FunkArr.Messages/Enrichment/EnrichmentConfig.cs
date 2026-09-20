namespace FunkArr.Messages.Enrichment;

public sealed record EnrichmentConfig(
    bool Enabled,
    EnrichmentMethod[] Methods,
    TitleMatchConfig Title,
    AirdateMatchConfig Airdate,
    RuntimeMatchConfig Runtime,
    YearMatchConfig Year);

public sealed record TitleMatchConfig(float Threshold);

public sealed record AirdateMatchConfig(int Tolerance);

public sealed record RuntimeMatchConfig(float Tolerance, RuntimeMode Mode);

public sealed record YearMatchConfig(int Tolerance);
