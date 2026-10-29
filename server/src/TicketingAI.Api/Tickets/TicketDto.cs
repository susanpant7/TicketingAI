namespace TicketingAI.Api.Tickets;

public record TicketDto(
    int Id,
    string Title,
    string Description,
    string Department,
    string ReportedBy,
    string ReportedByUsername,
    string AssignedTo,
    string AssignedToUsername,
    DateTime CreatedAt);
