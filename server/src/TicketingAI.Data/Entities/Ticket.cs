namespace TicketingAI.Data.Entities;

public class Ticket
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public DateTime CreatedAt { get; set; }

    public int ReportedById { get; set; }
    public User ReportedBy { get; set; } = null!;

    // The department of a ticket is the department of the person it is assigned to.
    public int AssignedToId { get; set; }
    public User AssignedTo { get; set; } = null!;
}
