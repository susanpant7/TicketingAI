using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Users;

// Password is optional: when empty, the existing password is kept.
public record UpdateUserRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    int DepartmentId,
    string? Password);
