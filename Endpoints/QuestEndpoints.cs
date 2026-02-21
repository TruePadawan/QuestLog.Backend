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
        var questGroup = app.MapGroup("/api/quests");

        questGroup.MapPost("/",
                async Task<IResult> (CreateQuestDto payload, QuestLogDbContext dbContext, ClaimsPrincipal user) =>
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

                    var quest = new Quest
                    {
                        Title = payload.Title,
                        Details = payload.Details,
                        DifficultyRating = payload.DifficultyRating,
                        Category = payload.Category,
                        Deadline = payload.Deadline,
                        Tags = payload.Tags,
                        Adventurer = adventurer,
                        CreatedAt = DateTime.UtcNow
                    };
                    await dbContext.AddAsync(quest);
                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<Quest>.Ok(quest), statusCode: StatusCodes.Status201Created);
                })
            .RequireAuthorization()
            .Produces<ApiResponse<Quest>>(201)
            .Produces<ApiResponse<object>>(400)
            .Produces<ApiResponse<object>>(401);

        questGroup.MapGet("/",
                async Task<IResult> (QuestLogDbContext dbContext, string? tag, string? category,
                    bool? completed, ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    var query = dbContext.Quests.Where(q => q.AdventurerId == userId).AsQueryable();
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

        questGroup.MapGet("/{questId:int}",
                async Task<IResult> (int questId, QuestLogDbContext dbContext, ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    var quest = await dbContext.Quests.FirstOrDefaultAsync(q =>
                        q.Id == questId && q.AdventurerId == userId);
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
                async Task<IResult> (int questId, UpdateQuestDto payload, QuestLogDbContext dbContext,
                    ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    var quest = await dbContext.Quests.FirstOrDefaultAsync(q =>
                        q.Id == questId && q.AdventurerId == userId);
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
                        if (payload.Completed)
                        {
                            quest.CompletedAt = DateTime.UtcNow;
                            // Update adventurer's XP'
                            var rewardXp = payload.DifficultyRating switch
                            {
                                DifficultyRating.Low => 200,
                                DifficultyRating.Medium => 500,
                                DifficultyRating.High => 1000,
                                _ => 0
                            };
                            await dbContext.Adventurers.Where(a => a.UserId == quest.AdventurerId)
                                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Xp, a => a.Xp + rewardXp));
                        }
                    }

                    quest.UpdatedAt = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    return TypedResults.Json(ApiResponse<Quest>.Ok(quest), statusCode: StatusCodes.Status200OK);
                })
            .RequireAuthorization()
            .Produces<ApiResponse<Quest>>(200)
            .Produces<ApiResponse<object>>(404);

        questGroup.MapDelete("/{questId:int}",
                async Task<IResult> (int questId, ClaimsPrincipal user, QuestLogDbContext dbContext) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    var rowsDeleted = await dbContext.Quests.Where(q => q.Id == questId && q.AdventurerId == userId)
                        .ExecuteDeleteAsync();
                    if (rowsDeleted == 0)
                    {
                        return TypedResults.Json(ApiResponse<object>.Fail("Quest not found"),
                            statusCode: StatusCodes.Status404NotFound);
                    }

                    return TypedResults.Json(ApiResponse<object>.Ok(null), statusCode: StatusCodes.Status200OK);
                }).RequireAuthorization()
            .Produces<ApiResponse<object>>(200)
            .Produces<ApiResponse<object>>(404);
    }
}