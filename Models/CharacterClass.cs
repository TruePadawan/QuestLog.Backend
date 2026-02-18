using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuestLog.Backend.Models;

[Table("CharacterClasses", Schema = "public")]
public class CharacterClass
{
    public int Id { get; init; }
    [MaxLength(30)] public required string Name { get; init; }

    public ICollection<ClassProgression> Progressions { get; set; } = [];
}