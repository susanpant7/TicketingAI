namespace TicketingAI.Api.Users;

public record UserDto(int Id, string Username, string FullName, string Email, int DepartmentId, string DepartmentName);
