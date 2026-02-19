using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Lib.Dtos;

public record CreateQuestDto
{
    [Required] public required string Title { get; init; }
    [Required] public JsonDocument? Details { get; init; }
    [Required] public DifficultyRating DifficultyRating { get; init; }
    [Required] public QuestCategory Category { get; init; }
    [Required] public DateTime Deadline { get; init; }
    [Required] public ICollection<string> Tags { get; init; } = [];
}