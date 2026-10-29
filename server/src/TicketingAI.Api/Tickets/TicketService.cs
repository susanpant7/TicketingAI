using Microsoft.Extensions.Options;
using TicketingAI.Api.Auth;
using TicketingAI.Data.Entities;

namespace TicketingAI.Api.Tickets;

public class TicketService(ITicketRepository repository, IOptions<AdminOptions> adminOptions) : ITicketService
{
    // Choosing the assignee and saving the ticket must not interleave between two requests, or both get the same person.
    private static readonly SemaphoreSlim AssignLock = new(1, 1);

    public Task<List<TicketDto>> GetAllAsync(int? userId) => repository.GetAllAsync(userId);

    public async Task<TicketResult> UpdateStatusAsync(int ticketId, int? employeeId, TicketStatus status)
    {
        var assigneeId = await repository.GetAssigneeIdAsync(ticketId);
        if (assigneeId is null)
            return new TicketResult(null, TicketError.NotFound);
        if (assigneeId != employeeId)
            return new TicketResult(null, TicketError.NotAssignee);

        await repository.SetStatusAsync(ticketId, status);
        return new TicketResult(await repository.GetByIdAsync(ticketId));
    }

    public async Task<TicketDto?> CreateAsync(int reportedById, int departmentId, string title, string description)
    {
        await AssignLock.WaitAsync();
        try
        {
            var assignee = await repository.GetNextAssigneeAsync(departmentId, reportedById, adminOptions.Value.Username);
            if (assignee is null)
                return null;

            var ticket = new Ticket
            {
                Title = title,
                Description = description,
                ReportedById = reportedById,
                AssignedToId = assignee.Id,
                CreatedAt = DateTime.UtcNow
            };
            await repository.AddAsync(ticket);
            return await repository.GetByIdAsync(ticket.Id);
        }
        finally
        {
            AssignLock.Release();
        }
    }
}
