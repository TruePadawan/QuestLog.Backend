using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestLog.Backend.Models;

[Table("Adventurers", Schema = "public")]
public class Adventurer
{
    [Key] public required string UserId { get; init; }
    [MaxLength(30)] public required string CharacterName { get; set; }
    public int CharacterClassId { get; init; }
    public required CharacterClass CharacterClass { get; init; }
    public int Xp { get; init; }
}