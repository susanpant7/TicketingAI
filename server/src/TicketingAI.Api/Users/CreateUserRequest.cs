using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Users;

public record CreateUserRequest(
    [Required, StringLength(50)] string Username,
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required] string Password,
    int DepartmentId);
