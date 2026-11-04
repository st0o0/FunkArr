using System.ComponentModel.DataAnnotations;

namespace FunkArr.Api.Models;

public sealed record MoveRequest([property: Range(0, int.MaxValue)] int Position, QueuePriority? Priority = null);
