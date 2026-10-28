namespace TicketingAI.Data.Entities;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}
