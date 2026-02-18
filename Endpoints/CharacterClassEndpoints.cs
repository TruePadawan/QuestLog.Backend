using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib;
using QuestLog.Backend.Lib.Dtos;

namespace QuestLog.Backend.Endpoints;

public static class CharacterClassEndpoints
{
    public static void MapCharacterClassEndpoints(this WebApplication app)
    {
        var characterClassGroup = app.MapGroup("/character-classes");

        characterClassGroup.MapGet("/", async (QuestLogDbContext dbContext) =>
            {
                var allClasses = await dbContext.CharacterClasses.Select(c => new CharacterClassDto { Name = c.Name })
                    .ToListAsync();
                var response = ApiResponse<List<CharacterClassDto>>.Ok(allClasses);
                return TypedResults.Json(response, statusCode: StatusCodes.Status200OK);
            })
            .Produces<ApiResponse<List<CharacterClassDto>>>(200);
    }
}