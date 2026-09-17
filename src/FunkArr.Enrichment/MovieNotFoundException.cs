namespace FunkArr.Enrichment;

public sealed class MovieNotFoundException : Exception
{
    public MovieNotFoundException() : base("Movie not found") { }
}
