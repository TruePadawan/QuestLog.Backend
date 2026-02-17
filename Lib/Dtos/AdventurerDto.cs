namespace QuestLog.Backend.Lib.Dtos;

public record AdventurerDto
{
    public string CharacterName { get; init; } = string.Empty;
    public string CharacterClass { get; init; } = string.Empty;
};