using QuestLog.Backend.Models;

namespace QuestLog.Backend.Lib;

public static class Utilities
{
    public static string CalculateTier(int xp, ICollection<ClassProgression> progressions)
    {
        var sortedProgressions = progressions.OrderByDescending(p => p.MinXp).ToList();
        foreach (var progression in sortedProgressions.Where(progression => progression.MinXp <= xp))
        {
            return progression.Tier;
        }

        return "Tarnished";
    }
}