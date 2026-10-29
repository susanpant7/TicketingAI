using System.ComponentModel.DataAnnotations;

namespace TicketingAI.Api.Departments;

public record DepartmentRequest([Required] string Name, string? Description, string? Contact);
