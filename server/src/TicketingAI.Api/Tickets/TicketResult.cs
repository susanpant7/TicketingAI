using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Tickets;

public enum TicketError
{
    None,
    NotFound,
    NotAssignee
}

public record TicketResult(TicketDto? Ticket, TicketError Error = TicketError.None);

public record UpdateTicketStatusRequest(TicketStatus Status);
