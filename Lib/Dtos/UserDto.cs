namespace QuestLog.Backend.Lib.Dtos;

public record UserDto
{
    public string Email { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
};