using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QuestLog.Backend.Models;

[Table("Quests", Schema = "public")]
public class Quest
{
    public int Id { get; init; }
    [MaxLength(50)] public required string Title { get; set; }
    public JsonDocument? Details { get; set; }
    public required DifficultyRating DifficultyRating { get; set; }
    public required QuestCategory Category { get; set; }
    public required DateTime Deadline { get; set; }
    public ICollection<string> Tags { get; set; } = [];
    public bool Completed { get; set; } = false;
    public string AdventurerId { get; init; }
    public required Adventurer Adventurer { get; init; }
}