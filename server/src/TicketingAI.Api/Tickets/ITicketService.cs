namespace TicketingAI.Api.Tickets;

public interface ITicketService
{
    /// <summary>All tickets for an admin (null user id), otherwise the tickets the user reported or was assigned.</summary>
    Task<List<TicketDto>> GetAllAsync(int? userId);

    /// <summary>Creates a ticket and assigns it to the next member of the department. Returns null if the department has no members.</summary>
    Task<TicketDto?> CreateAsync(int reportedById, int departmentId, string title, string description);
}
