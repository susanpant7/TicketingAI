using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32)]
    public string Key { get; init; } = "";

    [Required]
    public string Issuer { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int ExpiryMinutes { get; init; } = 60;
}
