namespace TicketingAI.Api.Departments;

public enum DepartmentError
{
    None,
    NotFound,
    NameTaken,
    HasUsers,
    Invalid
}

public record DepartmentResult(DepartmentDto? Department = null, DepartmentError Error = DepartmentError.None, string? Message = null);
