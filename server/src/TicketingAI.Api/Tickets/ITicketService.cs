using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Tickets;

public interface ITicketService
{
    /// <summary>All tickets for an admin (null user id), otherwise the tickets the user reported or was assigned.</summary>
    Task<List<TicketDto>> GetAllAsync(int? userId);

    /// <summary>Changes the status of a ticket. Only the person it is assigned to can do that.</summary>
    Task<TicketResult> UpdateStatusAsync(int ticketId, int? employeeId, TicketStatus status);

    /// <summary>Creates a ticket and assigns it to the next member of the department. Returns null if the department has no members.</summary>
    Task<TicketDto?> CreateAsync(int reportedById, int departmentId, string title, string description);
}
