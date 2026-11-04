using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models.Downloads;

public sealed record PriorityRequest([property: Required] QueuePriority Priority);
