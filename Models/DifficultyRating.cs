using System.Text.Json.Serialization;

namespace QuestLog.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyRating
{
    Low,
    Medium,
    High
}