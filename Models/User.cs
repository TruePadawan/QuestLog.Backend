using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace QuestLog.Backend.Models;

public class User : IdentityUser
{
    [MaxLength(30)] public string CharacterName { get; set; } = "Noob";
}