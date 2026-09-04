namespace FunkArr.Messages.MetadataResolver;

public interface IMovieResolutionResponse;

public sealed record MoviesResolved(
    MovieResolved[] Movies) : IMovieResolutionResponse;

public sealed record MovieResolutionFailed(
    string Reason) : IMovieResolutionResponse;
