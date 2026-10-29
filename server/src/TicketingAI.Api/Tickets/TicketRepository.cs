using Microsoft.EntityFrameworkCore;
using TicketingAI.Data.Entities;
using TicketingAI.Data.Persistence;

namespace TicketingAI.Api.Tickets;

public class TicketRepository(AppDbContext db) : ITicketRepository
{
    public Task<List<TicketDto>> GetAllAsync(int? userId) =>
        // Sorted before it is turned into DTOs, because EF Core cannot sort on a property of a constructed object.
        Dtos(db.Tickets
                .Where(t => userId == null || t.ReportedById == userId || t.AssignedToId == userId)
                .OrderByDescending(t => t.Id))
            .ToListAsync();

    public Task<TicketDto?> GetByIdAsync(int id) => Dtos(db.Tickets.Where(t => t.Id == id)).FirstOrDefaultAsync();

    public async Task<User?> GetNextAssigneeAsync(int departmentId, int reportedById, string adminUsername)
    {
        var members = await db.Users.Where(u => u.DepartmentId == departmentId && u.Username != adminUsername).OrderBy(u => u.Id).ToListAsync();
        if (members.Count == 0)
            return null;

        var candidates = members.Where(u => u.Id != reportedById).ToList();
        if (candidates.Count == 0)
            candidates = members;

        var lastAssigneeId = await db.Tickets
            .Where(t => t.AssignedTo.DepartmentId == departmentId)
            .OrderByDescending(t => t.Id)
            .Select(t => (int?)t.AssignedToId)
            .FirstOrDefaultAsync() ?? 0;

        return candidates.FirstOrDefault(u => u.Id > lastAssigneeId) ?? candidates[0];
    }

    public Task<int?> GetAssigneeIdAsync(int id) =>
        db.Tickets.Where(t => t.Id == id).Select(t => (int?)t.AssignedToId).FirstOrDefaultAsync();

    public Task SetStatusAsync(int id, TicketStatus status) =>
        db.Tickets.Where(t => t.Id == id).ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, status));

    public async Task AddAsync(Ticket ticket)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
    }

    private static IQueryable<TicketDto> Dtos(IQueryable<Ticket> tickets) =>
        tickets.Select(t => new TicketDto(
            t.Id, t.Title, t.Description, t.AssignedTo.Department.Name, t.Status,
            t.ReportedBy.FullName, t.ReportedBy.Username,
            t.AssignedTo.FullName, t.AssignedTo.Username,
            t.CreatedAt));
}
