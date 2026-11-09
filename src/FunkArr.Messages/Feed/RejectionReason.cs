namespace FunkArr.Messages.Feed;

public enum RejectionReason
{
    Unknown = 0,
    LowConfidence = 1,
    MissingEpisode = 2,
    MissingId = 3,
    UnknownSource = 4,
    MissingTimestamp = 5
}
