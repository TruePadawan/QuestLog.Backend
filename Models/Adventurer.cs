using System.ComponentModel.DataAnnotations;

namespace QuestLog.Backend.Models;

public class Adventurer
{
    public required string UserId { get; init; }
    [MaxLength(30)] public required string CharacterName { get; init; }
    public int CharacterClassId { get; init; }
    public required CharacterClass CharacterClass { get; init; }
}