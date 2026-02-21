using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib;
using QuestLog.Backend.Lib.Dtos;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Endpoints;

public static class AdventurerEndpoints
{
    public static void MapAdventurerEndpoints(this WebApplication app)
    {
        var adventurerGroup = app.MapGroup("/api/adventurers");

        adventurerGroup.MapPost("/",
                async (QuestLogDbContext dbContext, ClaimsPrincipal user, CreateAdventurerDto payload) =>
                {
                    var characterClass =
                        await dbContext.CharacterClasses.FirstOrDefaultAsync(c => c.Name == payload.CharacterClass);
                    if (characterClass == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Invalid character class"),
                            statusCode: StatusCodes.Status400BadRequest);
                    }

                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("User not logged in"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    await dbContext.Adventurers.AddAsync(new Adventurer
                    {
                        UserId = userId,
                        CharacterName = payload.CharacterName,
                        CharacterClass = characterClass
                    });
                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<object>.Ok(null), statusCode: StatusCodes.Status201Created);
                })
            .Produces<ApiResponse<object>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401);

        adventurerGroup.MapPatch("/",
                async Task<IResult> (QuestLogDbContext dbContext, ClaimsPrincipal user, UpdateAdventurerDto payload) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("User not logged in"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    var adventurer = await dbContext.Adventurers.FirstOrDefaultAsync(a => a.UserId == userId);
                    if (adventurer == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("No adventurer found for this user"),
                            statusCode: StatusCodes.Status404NotFound);
                    }

                    adventurer.CharacterName = payload.CharacterName;
                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<Adventurer>.Ok(adventurer),
                        statusCode: StatusCodes.Status200OK);
                }).RequireAuthorization()
            .Produces<ApiResponse<object>>(401)
            .Produces<ApiResponse<object>>(404)
            .Produces<ApiResponse<Adventurer>>(200);
    }
}