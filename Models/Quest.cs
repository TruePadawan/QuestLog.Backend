using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QuestLog.Backend.Models;

[Table("Quests", Schema = "public")]
public class Quest
{
    public int Id { get; init; }
    [MaxLength(50)] public required string Title { get; init; }
    public JsonDocument? Details { get; init; }
    public required DifficultyRating DifficultyRating { get; init; }
    public required QuestCategory Category { get; init; }
    public required DateTime Deadline { get; init; }
    public ICollection<string> Tags { get; init; } = [];
    public string AdventurerId { get; init; }
    public required Adventurer Adventurer { get; init; }
}