using System.ComponentModel.DataAnnotations;

namespace QuestLog.Backend.Models;

public class CharacterClass
{
    public int Id { get; init; }
    [MaxLength(30)] public required string Name { get; init; }
}