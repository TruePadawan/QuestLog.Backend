using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace QuestLog.Backend.Database;

public class User : IdentityUser
{
    [MaxLength(30)] public string CharacterName { get; set; } = "Noob";
}