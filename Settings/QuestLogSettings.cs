using System.ComponentModel.DataAnnotations;

namespace QuestLog.Backend.Settings;

public class QuestLogSettings
{
    [Required] public string FrontEndUrl { get; set; } = string.Empty;
}