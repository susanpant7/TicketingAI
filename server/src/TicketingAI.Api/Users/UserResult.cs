namespace TicketingAI.Api.Users;

public enum UserError
{
    None,
    NotFound,
    UsernameTaken,
    EmailTaken,
    InvalidDepartment
}

public record UserResult(UserDto? User, UserError Error = UserError.None);
