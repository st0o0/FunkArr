using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record PriorityRequest([property: Required] QueuePriority Priority);
