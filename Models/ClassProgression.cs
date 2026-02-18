using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestLog.Backend.Models;

[Table("ClassProgressions", Schema = "public")]
public class ClassProgression
{
    public int Id { get; init; }
    public int CharacterClassId { get; init; }

    [ForeignKey(nameof(CharacterClassId))] public CharacterClass CharacterClass { get; init; }

    [Required] [MaxLength(30)] public required string Tier { get; init; }
    [Required] public int MinXp { get; init; }
}