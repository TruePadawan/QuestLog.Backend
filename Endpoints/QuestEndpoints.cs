using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib;
using QuestLog.Backend.Lib.Dtos;
using QuestLog.Backend.Models;

namespace QuestLog.Backend.Endpoints;

public static class QuestEndpoints
{
    public static void MapQuestEndpoints(this WebApplication app)
    {
        var questGroup = app.MapGroup("/quests");

        questGroup.MapPost("/",
                async (CreateQuestDto payload, QuestLogDbContext dbContext, ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("User not logged in"),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    Console.WriteLine(userId);
                    var adventurer = await dbContext.Adventurers.FirstOrDefaultAsync(a => a.UserId == userId);
                    if (adventurer == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("No adventurer found for this user"),
                            statusCode: StatusCodes.Status404NotFound);
                    }

                    await dbContext.AddAsync(new Quest
                    {
                        Title = payload.Title,
                        Details = payload.Details,
                        DifficultyRating = payload.DifficultyRating,
                        Category = payload.Category,
                        Deadline = payload.Deadline,
                        Tags = payload.Tags,
                        Adventurer = adventurer
                    });
                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<object>.Ok(null), statusCode: StatusCodes.Status201Created);
                })
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401);
    }
}