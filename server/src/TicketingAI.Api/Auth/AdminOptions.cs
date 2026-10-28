using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Auth;

public class AdminOptions
{
    public const string SectionName = "Admin";

    [Required]
    public string Username { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}
