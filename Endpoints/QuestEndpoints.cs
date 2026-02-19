using System.Security.Claims;
using QuestLog.Backend.Database;
using QuestLog.Backend.Lib.Dtos;

namespace QuestLog.Backend.Endpoints;

public static class QuestEndpoints
{
    public static void MapQuestEndpoints(this WebApplication app)
    {
        var questGroup = app.MapGroup("/quests");

        questGroup.MapPost("/", async (CreateQuestDto payload, QuestLogDbContext dbContext, ClaimsPrincipal user) =>
        {

        });
    }
}