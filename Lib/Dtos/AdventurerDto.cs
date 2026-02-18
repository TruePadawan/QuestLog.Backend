using System.ComponentModel.DataAnnotations;

namespace QuestLog.Backend.Lib.Dtos;

public record AdventurerDetailsDto
{
    public required string CharacterName { get; init; }
    public required string CharacterClass { get; init; }
    public required int Xp { get; init; }
    public required string Tier { get; init; }
};

public record CreateAdventurerDto(
    [Required] string CharacterName,
    [Required] string CharacterClass
);