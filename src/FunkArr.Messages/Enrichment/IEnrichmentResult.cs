namespace FunkArr.Messages.Enrichment;

public interface IEnrichmentResult
{
    int Index { get; }
    float Confidence { get; }
    MatchMethod Method { get; }
}
