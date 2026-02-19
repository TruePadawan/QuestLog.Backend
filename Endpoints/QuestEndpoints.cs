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

        questGroup.MapGet("/",
                async Task<IResult> (QuestLogDbContext dbContext, string? tag, string? category,
                    bool? completed) =>
                {
                    var query = dbContext.Quests.AsQueryable();
                    if (completed.HasValue)
                    {
                        query = query.Where(q => q.Completed == completed.Value);
                    }

                    if (!string.IsNullOrEmpty(tag))
                    {
                        query = query.Where(q => q.Tags.Contains(tag));
                    }
                    else if (!string.IsNullOrEmpty(category))
                    {
                        query = query.Where(q => q.Category.ToString().ToUpper() == category.ToUpper());
                    }

                    var quests = await query.ToListAsync();
                    return TypedResults.Json(ApiResponse<List<Quest>>.Ok(quests), statusCode: StatusCodes.Status200OK);
                })
            .RequireAuthorization()
            .Produces<List<Quest>>(200);

        questGroup.MapGet("/{questId:int}", async Task<IResult> (int questId, QuestLogDbContext dbContext) =>
            {
                var quest = await dbContext.Quests.FirstOrDefaultAsync(q => q.Id == questId);
                if (quest == null)
                {
                    return TypedResults.Json(ApiResponse<object>.Fail("Quest not found"),
                        statusCode: StatusCodes.Status404NotFound);
                }

                return TypedResults.Json(ApiResponse<Quest>.Ok(quest), statusCode: StatusCodes.Status200OK);
            })
            .RequireAuthorization()
            .Produces<ApiResponse<object>>(404)
            .Produces<Quest>(200);

        questGroup.MapPatch("/{questId:int}",
                async Task<IResult> (int questId, UpdateQuestDto payload, QuestLogDbContext dbContext) =>
                {
                    var quest = await dbContext.Quests.FirstOrDefaultAsync(q => q.Id == questId);
                    if (quest == null)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Quest not found"),
                            statusCode: StatusCodes.Status404NotFound);
                    }

                    // Limit what can be updated after a quest is completed
                    quest.Title = payload.Title;
                    quest.Details = payload.Details;
                    quest.DifficultyRating = payload.DifficultyRating;
                    quest.Tags = payload.Tags;
                    if (!quest.Completed)
                    {
                        quest.Deadline = payload.Deadline;
                        quest.Category = payload.Category;
                        quest.Completed = payload.Completed;
                    }

                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<Quest>.Ok(quest), statusCode: StatusCodes.Status200OK);
                })
            .RequireAuthorization()
            .Produces<ApiResponse<Quest>>(200)
            .Produces<ApiResponse<object>>(404);
    }
}