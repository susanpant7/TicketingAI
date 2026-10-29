using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Tickets;

public interface ITicketRepository
{
    /// <summary>Newest first. With a user id, only the tickets reported by or assigned to that user; without one, all tickets.</summary>
    Task<List<TicketDto>> GetAllAsync(int? userId);

    Task<TicketDto?> GetByIdAsync(int id);

    /// <summary>
    /// The department member who should get the next ticket: members take turns in order of their id, continuing after the
    /// assignee of the department's latest ticket. The reporter is skipped unless they are the only member. A user with the admin's username is never chosen.
    /// Returns null if the department has no members.
    /// </summary>
    Task<User?> GetNextAssigneeAsync(int departmentId, int reportedById, string adminUsername);

    Task AddAsync(Ticket ticket);
}
