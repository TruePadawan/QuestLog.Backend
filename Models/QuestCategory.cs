using System.Text.Json.Serialization;

namespace QuestLog.Backend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuestCategory
{
    MainQuest,
    SideQuest,
    Tutorial
}