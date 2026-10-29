using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Tickets;

public record TicketDto(
    int Id,
    string Title,
    string Description,
    string Department,
    TicketStatus Status,
    string ReportedBy,
    string ReportedByUsername,
    string AssignedTo,
    string AssignedToUsername,
    DateTime CreatedAt);
