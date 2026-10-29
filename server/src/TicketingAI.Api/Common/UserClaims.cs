using System.Security.Claims;

namespace TicketingAI.Api.Common;

public static class UserClaims
{
    /// <summary>The id of the logged-in employee, or null for the admin (who is not a row in the Users table).</summary>
    public static int? GetEmployeeId(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || !int.TryParse(user.FindFirstValue("sub"), out var id) ? null : id;
}
